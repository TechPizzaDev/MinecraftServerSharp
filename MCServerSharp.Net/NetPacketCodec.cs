using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    public delegate OperationStatus NetPacketHandler(
        NetConnection connection,
        NetBinaryReader packetReader,
        NetPacketDecoder.PacketIdDefinition packetIdDefinition,
        out int messageLength);

    public delegate void NetLegacyServerListPingHandler(
        NetConnection connection,
        ClientLegacyServerListPing? ping);

    public partial class NetPacketCodec
    {
        private Dictionary<ClientPacketId, NetPacketHandler> PacketHandlers { get; } =
            new Dictionary<ClientPacketId, NetPacketHandler>();

        private NetPacketDecoder.PacketIdDefinition LegacyServerListPingPacketDefinition { get; set; }

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketDecoder Decoder { get; }
        public NetPacketEncoder Encoder { get; }

        public NetLegacyServerListPingHandler? LegacyServerListPingHandler { get; set; }

        #region Constructors

        public NetPacketCodec(RecyclableMemoryManager memoryManager)
        {
            MemoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            Decoder = new NetPacketDecoder();
            Encoder = new NetPacketEncoder();
        }

        #endregion

        #region SetupCoders

        public void SetupCoders()
        {
            SetupDecoder();
            SetupEncoder();
        }

        private void SetupDecoder()
        {
            Decoder.RegisterClientPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + Decoder.RegisteredTypeCount + " client packet types");

            Decoder.InitializePacketIdMaps(typeof(ClientPacketId).GetFields());

            Decoder.CreatePacketActions();
            if (!Decoder.TryGetPacketIdDefinition(ClientPacketId.LegacyServerListPing, out var definition))
                throw new InvalidOperationException(
                    $"Missing packet definition for \"{nameof(ClientPacketId.LegacyServerListPing)}\".");
            LegacyServerListPingPacketDefinition = definition;
        }

        private void SetupEncoder()
        {
            Encoder.RegisterServerPacketTypesFromCallingAssembly();

            Console.WriteLine("Registered " + Decoder.RegisteredTypeCount + " server packet types");

            Encoder.InitializePacketIdMaps(typeof(ServerPacketId).GetFields());

            Encoder.CreatePacketActions();
        }

        #endregion

        public void SetPacketHandler(ClientPacketId id, NetPacketHandler packetHandler)
        {
            if (packetHandler == null)
                throw new ArgumentNullException(nameof(packetHandler));

            if (PacketHandlers.ContainsKey(id))
                throw new ArgumentException($"A packet handler is already registered for \"{id}\".", nameof(id));

            PacketHandlers.Add(id, packetHandler);
        }

        public NetPacketHandler GetPacketHandler(ClientPacketId id)
        {
            if (!PacketHandlers.TryGetValue(id, out var packetHandler))
                throw new Exception($"Missing packet handler for \"{id}\".");

            return packetHandler;
        }

        public async Task EngageConnection(NetConnection connection, CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // As soon as the client connects, start receiving

            var readBuffer = new byte[1024 * 16];
            var readMemory = readBuffer.AsMemory();
            var socket = connection.Socket;

            var receiveBuffer = connection.ReceiveBuffer;
            var state = new ReceiveState(new NetBinaryReader(receiveBuffer), cancellationToken);

            try
            {
                int read;
                while ((read = await socket.ReceiveAsync(
                    readMemory, SocketFlags.None, state.CancellationToken).ConfigureAwait(false)) != 0)
                {
                    // We process by the message length (unless it's a legacy server list ping), 
                    // so don't worry if we received parts of the next message.

                    var readSlice = readMemory.Slice(0, read);
                    state.Reader.Seek(0, SeekOrigin.End);
                    state.Reader.BaseStream.Write(readSlice.Span);
                    state.Reader.Position = 0;
                    connection.BytesReceived += readSlice.Length;

                    OperationStatus handleStatus;
                    while ((handleStatus = HandlePacket(
                        connection, ref state, out VarInt totalMessageLength)) == OperationStatus.Done)
                    {
                        receiveBuffer.TrimStart(totalMessageLength);
                    }

                    if (handleStatus == OperationStatus.InvalidData)
                    {
                        // TODO: handle this state better
                        throw new InvalidDataException();
                    }
                }
            }
            catch (SocketException sockEx) when (sockEx.SocketErrorCode == SocketError.ConnectionReset)
            {
                // TODO: increment statistic?
            }
            catch (Exception ex)
            {
                connection.Kick(ex);
            }

            connection.Close(immediate: false);
        }

        public struct ReceiveState
        {
            public readonly NetBinaryReader Reader;
            public readonly CancellationToken CancellationToken;

            public ProtocolState? ProtocolOverride;

            public ReceiveState(NetBinaryReader reader, CancellationToken cancellationToken)
            {
                Reader = reader;
                CancellationToken = cancellationToken;

                ProtocolOverride = default;
            }
        }

        public OperationStatus HandlePacket(
            NetConnection connection, ref ReceiveState state, out VarInt totalPacketLength)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            totalPacketLength = default;

            if (state.Reader.PeekByte() == LegacyServerListPingPacketDefinition.RawId)
            {
                state.Reader.Position++;

                var legacyServerListPingStatus = ReadLegacyServerListPing(connection, state.Reader);
                if (legacyServerListPingStatus != OperationStatus.NeedMoreData)
                    connection.Close(immediate: false);

                return legacyServerListPingStatus;
            }

            var packetLengthStatus = state.Reader.Read(out VarInt packetLength, out int packetLengthBytes);
            if (packetLengthStatus != OperationStatus.Done)
                return packetLengthStatus;

            if (packetLength > NetManager.MaxClientPacketSize)
            {
                connection.Kick($"Packet length {packetLength} exceeds {NetManager.MaxClientPacketSize}.");
                return OperationStatus.Done;
            }

            totalPacketLength = packetLengthBytes + packetLength;
            if (state.Reader.Length < totalPacketLength)
                return OperationStatus.NeedMoreData;

            VarInt dataLength;
            Stream packetStream;
            if (connection.CompressionThreshold.HasValue)
            {
                var dataLengthStatus = state.Reader.Read(out dataLength, out int dataLengthBytes);
                if (dataLengthStatus != OperationStatus.Done)
                    return dataLengthStatus;

                if (dataLength != 0)
                {
                    var decompressionBuffer = connection.DecompressionBuffer;
                    using (var decompressor = new ZlibStream(state.Reader.BaseStream, CompressionMode.Decompress, true))
                    {
                        decompressionBuffer.SetLength(0);
                        decompressionBuffer.Position = 0;
                        decompressor.SpanCopyTo(decompressionBuffer);
                    }

                    if (dataLength != decompressionBuffer.Length)
                        return OperationStatus.InvalidData;

                    decompressionBuffer.Position = 0;
                    packetStream = decompressionBuffer;
                }
                else
                {
                    dataLength = packetLength - packetLengthBytes - dataLengthBytes;
                    packetStream = state.Reader.BaseStream;
                }
            }
            else
            {
                dataLength = packetLength - packetLengthBytes;
                packetStream = state.Reader.BaseStream;
            }

            var packetReader = new NetBinaryReader(packetStream);
            var packetIdStatus = packetReader.Read(out VarInt rawPacketId, out int packetIdBytes);
            if (packetIdStatus != OperationStatus.Done)
            {
                connection.Kick("Packet ID is incorrectly encoded.");
                return packetIdStatus;
            }

            if (!Decoder.TryGetPacketIdDefinition(
                state.ProtocolOverride ?? connection.ProtocolState, rawPacketId, out var packetIdDefinition))
            {
                connection.Kick($"Unknown packet ID \"{rawPacketId}\".");
                return OperationStatus.InvalidData;
            }

            var packetHandler = GetPacketHandler(packetIdDefinition.Id);
            var handlerStatus = packetHandler.Invoke(connection, packetReader, packetIdDefinition, out int readLength);
            if (handlerStatus != OperationStatus.Done)
                return handlerStatus;

            if (readLength > dataLength)
                throw new Exception("Packet handler read too much bytes.");

            return OperationStatus.Done;
        }

        private OperationStatus ReadLegacyServerListPing(NetConnection connection, NetBinaryReader reader)
        {
            try
            {
                ClientLegacyServerListPing? nPacket = default;

                if (reader.Length == 1)
                {
                    // beta ping
                }
                else if (reader.Length >= 2)
                {
                    var payloadStatus = reader.Read(out byte payload);
                    if (payloadStatus != OperationStatus.Done)
                        return payloadStatus;

                    if (payload != 0x01)
                        return OperationStatus.InvalidData;

                    if (reader.Length > 2)
                    {
                        var packetStatus = connection.ReadPacket(reader, out ClientLegacyServerListPing packet, out _);
                        if (packetStatus != OperationStatus.Done)
                            return packetStatus;

                        nPacket = packet;
                    }
                }
                else if (reader.Length == 0)
                    throw new InvalidOperationException();

                LegacyServerListPingHandler?.Invoke(connection, nPacket);

                return OperationStatus.Done;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return OperationStatus.InvalidData;
            }
        }
    }
}
