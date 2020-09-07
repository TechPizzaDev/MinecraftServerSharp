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
    public delegate int PacketHandlerDelegate(
        NetConnection connection, int rawPacketId, NetPacketDecoder.PacketIdDefinition packetIdDefinition);

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

        public Task AddConnection(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // TODO: cancellation token

            // As soon as the client connects, start receiving

            return Task.Run(async () =>
            {
                var buffer = new byte[1024 * 16];
                var memory = buffer.AsMemory();
                var socket = connection.Socket;

                var state = new ReceiveState(connection.BufferReader);

                try
                {
                    int read;
                    while ((read = await socket.ReceiveAsync(memory, SocketFlags.None)) != 0)
                    {
                        int packetRead = ProcessReceive(connection, memory.Slice(0, read), ref state);
                        if (packetRead == 0)
                            break;

                        if (packetRead == -1)
                            throw new Exception("Failed to read packet.");

                        connection.ReceiveBuffer.TrimStart(packetRead);
                        state.ResetForPacket();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    connection.Kick("Server Error: \n" + ex.ToString().Replace("\r", ""));
                }

                connection.Close(immediate: false);
            });
        }

        public struct ReceiveState
        {
            public readonly NetBinaryReader Reader;

            public ProtocolState? ProtocolOverride;
            public int ReceivedLength;
            public int ReceivedLengthBytes;

            public ReceiveState(NetBinaryReader reader)
            {
                Reader = reader;
                ProtocolOverride = default;
                ReceivedLength = -1;
                ReceivedLengthBytes = -1;
            }

            public void ResetForPacket()
            {
                ReceivedLength = -1;
                ReceivedLengthBytes = -1;
            }
        }

        public int ProcessReceive(NetConnection connection, ref ReceiveState state)
        {
            while (state.Reader.Length > 0)
            {
                state.Reader.Position = 0;
                if (state.ReceivedLength == -1)
                {
                    if (state.Reader.PeekByte() == LegacyServerListPingPacketDefinition.RawId)
                    {
                        state.Reader.Position++;
                        if (ReadLegacyServerListPing(connection) != OperationStatus.NeedMoreData)
                        {
                            connection.Close(immediate: false);
                            return -1;
                        }
                    }
                    else if (state.Reader.Read(
                        out VarInt messageLength, out int messageLengthBytes) == OperationStatus.Done)
                    {
                        state.ReceivedLength = messageLength;
                        state.ReceivedLengthBytes = messageLengthBytes;
                    }
                }

                if (state.ReceivedLength != -1 &&
                    state.Reader.Length >= state.ReceivedLength)
                {
                    if (!ValidatePacketAndGetId(
                        connection, ref state, out var rawPacketId, out var packetIdDefinition))
                        return -1;

                    var packetHandler = GetPacketHandler(packetIdDefinition.Id);
                    return packetHandler.Invoke(connection, rawPacketId, packetIdDefinition);
                }
                else
                {
                    break;
                }
            }
            return 0;
        }

        public int ProcessReceive(
            NetConnection connection, Memory<byte> data, ref ReceiveState state)
        {
            // TODO: this only reads uncompressed packets for now, 
            // this will require slight change when compressed packets are implemented

            // We process by the message length (unless it's a legacy server list ping), 
            // so don't worry if we received parts of the next message.
            connection.BytesReceived += data.Length;
            connection.ReceiveBuffer.Seek(0, SeekOrigin.End);
            connection.ReceiveBuffer.Write(data.Span);

            return ProcessReceive(connection, ref state);
        }

        public void FlushLoopbackBuffer(NetConnection connection)
        {
            var loopbackBuffer = connection.LoopbackBuffer;

            var state = new ReceiveState(new NetBinaryReader(loopbackBuffer))
            {
                ProtocolOverride = ProtocolState.Loopback
            };

            int totalPacketRead = 0;
            int packetRead;
            while ((packetRead = ProcessReceive(connection, ref state)) > 0)
            {
                state.ResetForPacket();
                totalPacketRead += packetRead;
            }
            connection.ReceiveBuffer.TrimStart(totalPacketRead);
        }

        public async Task<NetSendState> FlushSendBuffer(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var sendBuffer = connection.SendBuffer;
            int length = (int)sendBuffer.Length;
            if (length > 0 && connection.ProtocolState != ProtocolState.Disconnected)
            {
                int toWrite = length;
                int block = 0;
                while (toWrite > 0 && connection.ProtocolState != ProtocolState.Closing)
                {
                    var buffer = sendBuffer.GetBlock(block);
                    int blockLength = Math.Min(sendBuffer.BlockSize, toWrite);

                    var data = buffer.Slice(0, blockLength);
                    int write = await connection.Socket.SendAsync(data, SocketFlags.None);
                    if (write == 0)
                    {
                        connection.Close(immediate: false);
                        return NetSendState.Closing;
                    }

                    connection.BytesSent += write;
                    toWrite -= write;
                    block++;
                }

                connection.SendBuffer.TrimStart(length);
            }
            return NetSendState.FullSend;
        }

        private bool ValidatePacketAndGetId(
            NetConnection connection,
            ref ReceiveState state,
            out VarInt rawPacketId,
            out NetPacketDecoder.PacketIdDefinition definition)
        {
            if (state.Reader.Read(
                out rawPacketId, out int packetIdBytes) != OperationStatus.Done)
            {
                connection.Kick("Packet ID is incorrectly encoded.");
                definition = default;
                return false;
            }

            int packetLength = state.ReceivedLength - packetIdBytes;
            if (packetLength > NetManager.MaxClientPacketSize)
            {
                connection.Kick(
                    $"Packet length {packetLength} exceeds {NetManager.MaxClientPacketSize}.");
                definition = default;
                return false;
            }

            if (!Decoder.TryGetPacketIdDefinition(
                state.ProtocolOverride ?? connection.ProtocolState, rawPacketId, out definition))
            {
                connection.Kick($"Unknown packet ID \"{rawPacketId}\".");
                return false;
            }

            return true;
        }

        private OperationStatus ReadLegacyServerListPing(NetConnection connection)
        {
            try
            {
                ClientLegacyServerListPing? nPacket = default;

                var reader = connection.BufferReader;
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
                        var (packetStatus, length) = connection.ReadPacket(out ClientLegacyServerListPing packet);
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
