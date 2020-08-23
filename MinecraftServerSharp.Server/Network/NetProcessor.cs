using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using MinecraftServerSharp.NBT;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Network.Packets.Client;
using MinecraftServerSharp.Utility;
using MinecraftServerSharp.World;

namespace MinecraftServerSharp.Network
{
    public partial class NetProcessor
    {
        public const int BlockSize = 1024 * 16;
        public const int BlockMultiple = BlockSize * 16;
        public const int MaxBufferSize = BlockMultiple * 16;

        // These fit pretty well with the memory block sizes.
        public const int MaxServerPacketSize = 2097152;
        public const int MaxClientPacketSize = 32768;

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketDecoder PacketDecoder { get; }
        public NetPacketEncoder PacketEncoder { get; }
        private NetPacketDecoder.PacketIdDefinition LegacyServerListPingPacketDefinition { get; set; }

        // TODO: move these somewhere
        public static int ProtocolVersion { get; } = 578;
        public static MinecraftVersion GameVersion { get; } = new MinecraftVersion(1, 15, 2);
        public static bool Config_AppendGameVersionToBetaStatus { get; } = true;

        #region Constructors

        public NetProcessor(int blockSize, int blockMultiple, int maxBufferSize)
        {
            if (maxBufferSize < Math.Max(MaxClientPacketSize, MaxServerPacketSize))
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

            MemoryManager = new RecyclableMemoryManager(blockSize, blockMultiple, maxBufferSize);
            PacketDecoder = new NetPacketDecoder();
            PacketEncoder = new NetPacketEncoder();
        }

        public NetProcessor() : this(BlockSize, BlockMultiple, MaxBufferSize)
        {
        }

        #endregion

        #region SetupCodecs

        public void SetupCodecs()
        {
            SetupDecoder();
            SetupEncoder();
        }

        private void SetupDecoder()
        {
            PacketDecoder.RegisterClientPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " client packet types");

            PacketDecoder.InitializePacketIdMaps();

            PacketDecoder.CreateCodecDelegates();

            if (!PacketDecoder.TryGetPacketIdDefinition(ClientPacketId.LegacyServerListPing, out var definition))
                throw new InvalidOperationException(
                    $"Missing packet definition for \"{nameof(ClientPacketId.LegacyServerListPing)}\".");
            LegacyServerListPingPacketDefinition = definition;
        }

        private void SetupEncoder()
        {
            PacketEncoder.RegisterServerPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " server packet types");

            PacketEncoder.InitializePacketIdMaps();

            PacketEncoder.CreateCodecDelegates();
        }

        #endregion

        public void AddConnection(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            connection.ReceiveEvent.Completed += (s, e) => ProcessReceive((NetConnection)e.UserToken);
            connection.SendEvent.Completed += (s, e) => ProcessSend((NetConnection)e.UserToken);

            // As soon as the client connects, start receiving
            if (!connection.Socket.ReceiveAsync(connection.ReceiveEvent))
                ProcessReceive(connection);
        }

        private void ProcessReceive(NetConnection connection)
        {
            // TODO: this only reads uncompressed packets for now, 
            // this will require slight change when compressed packets are implemented

            try
            {
                var re = connection.ReceiveEvent;
                var reader = connection.Reader;

                AfterReceive:
                if (re.SocketError != SocketError.Success ||
                    re.BytesTransferred == 0) // 0 == closed connection
                {
                    connection.Close(immediate: true);
                    return;
                }

                // We process by the message length (unless it's a legacy server list ping), 
                // so don't worry if we received parts of the next message.
                connection.BytesReceived += re.BytesTransferred;
                connection.ReceiveBuffer.Seek(0, SeekOrigin.End);
                connection.ReceiveBuffer.Write(re.MemoryBuffer.Slice(0, re.BytesTransferred).Span);

                while (reader.Length > 0)
                {
                    reader.Position = 0;
                    if (connection.ReceivedLength == -1)
                    {
                        if (reader.PeekByte() == LegacyServerListPingPacketDefinition.RawId)
                        {
                            reader.Position++;
                            if (ReadLegacyServerListPing(connection) != OperationStatus.NeedMoreData)
                            {
                                connection.Close(immediate: false);
                                return;
                            }
                        }
                        else if (reader.Read(
                            out VarInt messageLength, out int messageLengthBytes) == OperationStatus.Done)
                        {
                            connection.ReceivedLength = messageLength;
                            connection.ReceivedLengthBytes = messageLengthBytes;
                        }
                    }

                    if (connection.ReceivedLength != -1 &&
                        reader.Length >= connection.ReceivedLength)
                    {
                        if (!ValidatePacketAndGetId(connection, out var rawPacketId, out var packetIdDefinition))
                            return;

                        // TODO: add packet handlers that use typed delegates and are routed
                        // by ClientPacketId and the ID on the PacketStruct attribute

                        if (packetIdDefinition.Id == ClientPacketId.Handshake)
                        {
                            if (connection.ReadPacket<ClientHandshake>(
                                out var handshakePacket).Status == OperationStatus.Done)
                            {
                                if (handshakePacket.NextState != ProtocolState.Status &&
                                    handshakePacket.NextState != ProtocolState.Login)
                                    return;

                                connection.State = handshakePacket.NextState;
                            }
                        }
                        else if (packetIdDefinition.Id == ClientPacketId.Request)
                        {
                            if (connection.ReadPacket<ClientRequest>(
                                out var requestPacket).Status == OperationStatus.Done)
                            {
                                // TODO make these dynamic
                                var jsonResponse = new Utf8String(File.ReadAllText(".\\..\\..\\..\\..\\omegalul.json")
                                    .Replace("%version%", GameVersion.ToString(), StringComparison.OrdinalIgnoreCase)
                                    .Replace("\"%versionID%\"", ProtocolVersion.ToString(NumberFormatInfo.InvariantInfo), StringComparison.OrdinalIgnoreCase)
                                    .Replace("\"%max%\"", 20.ToString(NumberFormatInfo.InvariantInfo), StringComparison.OrdinalIgnoreCase)
                                    .Replace("\"%online%\"", 0.ToString(NumberFormatInfo.InvariantInfo), StringComparison.OrdinalIgnoreCase));
                                var answer = new ServerResponse(jsonResponse);

                                connection.EnqueuePacket(answer);
                            }
                        }
                        else if (packetIdDefinition.Id == ClientPacketId.Ping)
                        {
                            if (connection.ReadPacket<ClientPing>(
                                out var pingPacket).Status == OperationStatus.Done)
                            {
                                var answer = new ServerPong(pingPacket.Payload);
                                connection.EnqueuePacket(answer);
                            }
                        }
                        else if (packetIdDefinition.Id == ClientPacketId.LoginStart)
                        {
                            if (connection.ReadPacket<ClientLoginStart>(
                                out var loginStartPacket).Status == OperationStatus.Done)
                            {
                                var uuid = new UUID(0, 1);

                                var name = loginStartPacket.Name;
                                var answer = new ServerLoginSuccess(uuid.ToUtf8String(), name);
                                connection.UserName = name;

                                connection.EnqueuePacket(answer);

                                connection.State = ProtocolState.Play;

                                var playerId = new EntityId(69);

                                connection.EnqueuePacket(new ServerJoinGame(
                                    playerId.Value, 3, 0, 0, 0, "default", 8, false, true));

                                connection.EnqueuePacket(new ServerPluginMessage(
                                    new Utf8String("minecraft:brand"), new Utf8String("MinecraftServerSharp")));

                                connection.EnqueuePacket(new ServerSpawnPosition(
                                    new Position(0, 16, 0)));

                                connection.EnqueuePacket(new ServerPlayerPositionLook(
                                    0, 16, 0, 0, 0, ServerPlayerPositionLook.PositionRelatives.None, 1337));

                                var dimension = new Dimension();
                                var chunk1 = new Chunk(0, 0, dimension);
                                var chunk2 = new Chunk(0, 1, dimension);
                                var chunk3 = new Chunk(1, 0, dimension);
                                var chunk4 = new Chunk(1, 1, dimension);
                                var chunkData1 = new ServerChunkData(chunk1, true);
                                var chunkData2 = new ServerChunkData(chunk2, true);
                                var chunkData3 = new ServerChunkData(chunk3, true);
                                var chunkData4 = new ServerChunkData(chunk4, true);
                                connection.EnqueuePacket(chunkData1);
                                connection.EnqueuePacket(chunkData2);
                                connection.EnqueuePacket(chunkData3);
                                connection.EnqueuePacket(chunkData4);
                            }
                        }
                        else if(packetIdDefinition.Id == ClientPacketId.TeleportConfirm)
                        {
                            if(connection.ReadPacket<ClientTeleportConfirm>(
                                out var teleportConfirmPacket).Status == OperationStatus.Done)
                            {
                                Console.WriteLine("Teleport Confirm: Id " + teleportConfirmPacket.TeleportId);
                            }
                        }
                        else if (packetIdDefinition.Id == ClientPacketId.PlayerPosition)
                        {
                            if (connection.ReadPacket<ClientPlayerPosition>(
                                  out var playerPositionPacket).Status == OperationStatus.Done)
                            {
                                Console.WriteLine(
                                  "Player Position:" +
                                  " X" + playerPositionPacket.X +
                                  " Y" + playerPositionPacket.FeetY +
                                  " Z" + playerPositionPacket.Z);
                            }
                        }
                        else if (packetIdDefinition.Id == ClientPacketId.ChatMessage)
                        {
                            if (connection.ReadPacket<ClientChat>(
                                     out var chatPacket).Status == OperationStatus.Done)
                            {
                                // TODO broadcast to everyone
                                Console.WriteLine("<" + connection.UserName + ">: " + chatPacket.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine(
                                "(" + connection.ReceivedLengthBytes + ") " +
                                connection.ReceivedLength + ": " +
                                packetIdDefinition.Id);
                        }

                        connection.TrimFirstReceivedMessage();
                    }
                    else
                    {
                        break;
                    }
                }

                if (!connection.Socket.ReceiveAsync(re))
                    goto AfterReceive;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Kick("Server Error: \n" + ex);
            }
        }

        private void ProcessSend(NetConnection connection)
        {
            try
            {
                var se = connection.SendEvent;
                if (se.SocketError != SocketError.Success ||
                    se.BytesTransferred == 0) // 0 == closed connection
                {
                    connection.Close(immediate: true);
                    return;
                }

                connection.BytesSent += se.BytesTransferred;
                connection.TrimSendBufferStart(se.BytesTransferred);

                FlushSendBuffer(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Close(immediate: true);
            }
        }

        public void FlushSendBuffer(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            lock (connection.SendMutex)
            {
                int length = (int)connection.SendBuffer.Length;
                if (length <= 0 ||
                    connection.State == ProtocolState.Disconnected)
                    return;

                var buffer = connection.SendBuffer.GetBlock(0);
                int blockLength = Math.Min(connection.SendBuffer.BlockSize, length);
                connection.SendEvent.SetBuffer(buffer.Slice(0, blockLength));
                Console.WriteLine("sent " + blockLength);

                if (!connection.Socket.SendAsync(connection.SendEvent))
                    ProcessSend(connection);
            }
        }

        private bool ValidatePacketAndGetId(
            NetConnection connection,
            out VarInt rawPacketId,
            out NetPacketDecoder.PacketIdDefinition definition)
        {
            if (connection.Reader.Read(
                out rawPacketId, out int packetIdBytes) != OperationStatus.Done)
            {
                connection.Kick("Packet ID is incorrectly encoded.");
                definition = default;
                return false;
            }

            int packetLength = connection.ReceivedLength - packetIdBytes;
            if (packetLength > MaxClientPacketSize)
            {
                connection.Kick(
                    $"Packet length {packetLength} exceeds {MaxClientPacketSize}.");
                definition = default;
                return false;
            }

            if (!PacketDecoder.TryGetPacketIdDefinition(
                connection.State, rawPacketId, out definition))
            {
                connection.Kick($"Unknown packet ID \"{rawPacketId}\".");
                return false;
            }

            return true;
        }

        private static OperationStatus ReadLegacyServerListPing(NetConnection connection)
        {
            try
            {
                bool isBeta = false;
                string motd = "A minecraft server";

                var reader = connection.Reader;
                if (reader.Length == 1)
                {
                    isBeta = true;
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

                        // TODO: do stuff with packet
                    }
                }
                else if (reader.Length == 0)
                    throw new InvalidOperationException();

                if (isBeta && Config_AppendGameVersionToBetaStatus)
                    motd = motd + " - " + GameVersion;

                var answer = new ServerLegacyServerListPong(
                    isBeta, ProtocolVersion, GameVersion, motd, 0, 100);

                connection.EnqueuePacket(answer);

                connection.State = ProtocolState.Closing;

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
