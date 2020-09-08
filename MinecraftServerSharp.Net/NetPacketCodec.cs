using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    public delegate OperationStatus PacketHandlerDelegate(
        NetConnection connection,
        NetPacketDecoder.PacketIdDefinition packetIdDefinition,
        out int messageLength);

    public delegate void LegacyServerListPingHandlerDelegate(
        NetConnection connection, ClientLegacyServerListPing? ping);

    public partial class NetPacketCodec
    {
        private Dictionary<ClientPacketId, PacketHandlerDelegate> PacketHandlers { get; } =
            new Dictionary<ClientPacketId, PacketHandlerDelegate>();

        private NetPacketDecoder.PacketIdDefinition LegacyServerListPingPacketDefinition { get; set; }

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketDecoder Decoder { get; }
        public NetPacketEncoder Encoder { get; }

        public LegacyServerListPingHandlerDelegate? LegacyServerListPingHandler { get; set; }

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

            Decoder.CreateCoderDelegates();
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

            Encoder.CreateCoderDelegates();
        }

        #endregion

        public void SetPacketHandler(ClientPacketId id, PacketHandlerDelegate packetHandler)
        {
            if (packetHandler == null)
                throw new ArgumentNullException(nameof(packetHandler));

            if (PacketHandlers.ContainsKey(id))
                throw new ArgumentException($"A packet handler is already registered for \"{id}\".", nameof(id));

            PacketHandlers.Add(id, packetHandler);
        }

        public PacketHandlerDelegate GetPacketHandler(ClientPacketId id)
        {
            if (!PacketHandlers.TryGetValue(id, out var packetHandler))
                throw new Exception($"Missing packet handler for \"{id}\".");
            return packetHandler;
        }

        public async Task EngageConnection(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // TODO: cancellation token

            // As soon as the client connects, start receiving

            var readBuffer = new byte[1024 * 16];
            var readMemory = readBuffer.AsMemory();
            var socket = connection.Socket;

            var receiveBuffer = connection.ReceiveBuffer;
            var state = new ReceiveState(new NetBinaryReader(receiveBuffer));

            try
            {
                int read;
                while ((read = await socket.ReceiveAsync(readMemory, SocketFlags.None)) != 0)
                {
                    // TODO: this only reads uncompressed packets for now, 
                    // this will require slight change when compressed packets are implemented

                    // We process by the message length (unless it's a legacy server list ping), 
                    // so don't worry if we received parts of the next message.

                    var readSlice = readMemory.Slice(0, read);
                    state.Reader.Seek(0, SeekOrigin.End);
                    state.Reader.BaseStream.Write(readSlice.Span);
                    connection.BytesReceived += readSlice.Length;

                    state.Reader.Position = 0;

                    OperationStatus handleStatus;
                    while ((handleStatus = HandlePacket(
                        connection, ref state, out VarInt totalMessageLength)) == OperationStatus.Done &&
                        connection.ProtocolState != ProtocolState.Closing)
                    {
                        receiveBuffer.TrimStart(totalMessageLength);
                    }

                    if (handleStatus == OperationStatus.InvalidData)
                    {
                        // TODO: do something more?
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Kick(ex);
            }

            connection.Close(immediate: false);
        }

        public struct ReceiveState
        {
            public readonly NetBinaryReader Reader;

            public ProtocolState? ProtocolOverride;

            public ReceiveState(NetBinaryReader reader)
            {
                Reader = reader;
                ProtocolOverride = default;
            }
        }

        public OperationStatus HandlePacket(
            NetConnection connection, ref ReceiveState state, out VarInt totalMessageLength)
        {
            totalMessageLength = default;

            if (state.Reader.PeekByte() == LegacyServerListPingPacketDefinition.RawId)
            {
                state.Reader.Position++;

                var legacyServerListPingStatus = ReadLegacyServerListPing(connection, state.Reader);
                if (legacyServerListPingStatus != OperationStatus.NeedMoreData)
                    connection.Close(immediate: false);

                return legacyServerListPingStatus;
            }

            var messageLengthStatus = state.Reader.Read(out VarInt packetLength, out int packetLengthBytes);
            if (messageLengthStatus != OperationStatus.Done)
                return messageLengthStatus;

            if (packetLength > NetManager.MaxClientPacketSize)
            {
                connection.Kick($"Packet length {packetLength} exceeds {NetManager.MaxClientPacketSize}.");
                return OperationStatus.Done;
            }

            totalMessageLength = packetLengthBytes + packetLength;
            if (state.Reader.Length < totalMessageLength)
                return OperationStatus.NeedMoreData;

            var packetIdStatus = state.Reader.Read(out VarInt rawPacketId, out int packetIdBytes);
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
            var handlerStatus = packetHandler.Invoke(connection, packetIdDefinition, out int readLength);
            if (handlerStatus != OperationStatus.Done)
                return handlerStatus;

            if (readLength > packetLength)
                throw new Exception("Packet handler read too much bytes.");

            return OperationStatus.Done;
        }

        public async Task<NetSendState> FlushSendBuffer(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var sendBuffer = connection.SendBuffer;
            int length = (int)sendBuffer.Length;
            if (length > 0 && connection.ProtocolState != ProtocolState.Disconnected)
            {
                int left = length;
                int block = 0;
                while (left > 0)
                {
                    var buffer = sendBuffer.GetBlock(block);
                    int blockLength = Math.Min(sendBuffer.BlockSize, left);

                    var data = buffer.Slice(0, blockLength);
                    int write = await connection.Socket.SendAsync(data, SocketFlags.None);
                    if (write == 0)
                    {
                        connection.Close(immediate: false);
                        return NetSendState.Closing;
                    }

                    connection.BytesSent += write;
                    left -= write;
                    block++;
                }

                connection.SendBuffer.TrimStart(length);
            }
            return NetSendState.FullSend;
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
                        var packetStatus = connection.ReadPacket(out ClientLegacyServerListPing packet, out _);
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
