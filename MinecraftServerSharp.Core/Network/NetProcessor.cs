using System;
using System.IO;
using System.Net.Sockets;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Utility;

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
        public static int ProtocolVersion { get; } = 498;
        public static MinecraftVersion GameVersion { get; } = new MinecraftVersion(1, 14, 4);
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

        #region SetupCoders

        public void SetupCoders()
        {
            SetupDecoder();
            SetupEncoder();
        }

        private void SetupDecoder()
        {
            PacketDecoder.RegisterClientPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " client packet types");

            PacketDecoder.InitializePacketIdMaps();

            PacketDecoder.CreateCoderDelegates();

            if (!PacketDecoder.TryGetPacketIdDefinition(ClientPacketID.LegacyServerListPing, out var definition))
                throw new InvalidOperationException(
                    $"Missing packet definition for \"{nameof(ClientPacketID.LegacyServerListPing)}\".");
            LegacyServerListPingPacketDefinition = definition;
        }

        private void SetupEncoder()
        {
            PacketEncoder.RegisterServerPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " server packet types");

            PacketEncoder.InitializePacketIdMaps();

            PacketEncoder.CreateCoderDelegates();
        }

        #endregion

        public void AddConnection(NetConnection connection)
        {
            connection.ReceiveEvent.Completed += (s, e) => ProcessReceive((NetConnection)e.UserToken);
            connection.SendEvent.Completed += (s, e) => ProcessSend((NetConnection)e.UserToken);

            // As soon as the client is connected, post a receive to the connection
            if (!connection.Socket.ReceiveAsync(connection.ReceiveEvent))
                ProcessReceive(connection);
        }

        private void ProcessReceive(NetConnection connection)
        {
            // TODO: this only reads uncompressed packets for now, 
            // this will require slight change when compressed packets are implemented

            try
            {
                var e = connection.ReceiveEvent;
                var reader = connection.Reader;

            AfterReceive:
                if (e.SocketError != SocketError.Success ||
                    e.BytesTransferred == 0)
                {
                    connection.Close();
                    return;
                }

                // We process by the message length (unless it's a legacy server list ping), 
                // so don't worry if we received parts of the next message.
                connection.ReceiveBuffer.Seek(0, SeekOrigin.End);
                connection.ReceiveBuffer.Write(e.MemoryBuffer.Span.Slice(0, e.BytesTransferred));

                if (reader.Length > 0)
                {
                    reader.Seek(0, SeekOrigin.Begin);
                    if (connection.ReceivedLength == -1)
                    {
                        if (reader.ReadByte() == LegacyServerListPingPacketDefinition.RawID)
                        {
                            var readCode = ReadLegacyServerListPing(connection);
                            if (readCode == ReadCode.Ok ||
                                readCode == ReadCode.InvalidData)
                            {
                                connection.Close();
                                return;
                            }
                        }
                        else
                        {
                            reader.Seek(0, SeekOrigin.Begin);
                            if (reader.Read(out VarInt messageLength, out int messageLengthBytes) == ReadCode.Ok)
                            {
                                connection.ReceivedLength = messageLength;
                                connection.ReceivedLengthBytes = messageLengthBytes;
                            }
                        }
                    }

                    if (connection.ReceivedLength != -1 &&
                        reader.Length >= connection.ReceivedLength)
                    {
                        if (!ValidatePacket(connection, out var rawPacketId, out var packetIdDefinition))
                            return;

                        if (packetIdDefinition.ID == ClientPacketID.Handshake)
                        {
                            if (connection.ReadPacket<ClientHandshake>(out var handshakePacket).Code == ReadCode.Ok)
                            {
                                Console.WriteLine("owo");
                            }
                        }

                        Console.WriteLine(
                            "(" + connection.ReceivedLengthBytes + ") " +
                            connection.ReceivedLength + ": " +
                            packetIdDefinition.ID);

                        connection.TrimFirstReceivedMessage();
                    }
                }

                if (!connection.Socket.ReceiveAsync(e))
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
                var e = connection.SendEvent;

            AfterSend:
                if (e.SocketError != SocketError.Success)
                {
                    connection.Close();
                }
                else
                {
                    connection.TrimSendBuffer(e.BytesTransferred);

                    int nextSendLength = (int)connection.SendBuffer.Length;
                    if (nextSendLength == 0)
                        return;

                    // TODO: add sending by block instead of using GetBuffer()
                    e.SetBuffer(connection.SendBuffer.GetBuffer(), 0, nextSendLength);
                    if (!connection.Socket.SendAsync(e))
                        goto AfterSend;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Close();
            }
        }

        private bool ValidatePacket(
            NetConnection connection,
            out VarInt rawPacketId, out NetPacketDecoder.PacketIdDefinition definition)
        {
            if (connection.Reader.Read(out rawPacketId, out int packetIdBytes) != ReadCode.Ok)
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

        private ReadCode ReadLegacyServerListPing(NetConnection connection)
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
                    var payloadRead = reader.Read(out byte payload);
                    if (payloadRead != ReadCode.Ok)
                        return payloadRead;

                    if (payload != 0x01)
                        return ReadCode.InvalidData;

                    if (reader.Length > 2)
                    {
                        var (packetRead, length) = connection.ReadPacket(out ClientLegacyServerListPing packet);
                        if (packetRead != ReadCode.Ok)
                            return packetRead;
                    }
                }
                else
                {
                    // Should typically not throw.
                    throw new InvalidOperationException("Nothing to read.");
                }

                if (isBeta && Config_AppendGameVersionToBetaStatus)
                    motd = motd + " - " + GameVersion;

                var answer = new ServerLegacyServerListPong(
                    isBeta, ProtocolVersion, GameVersion, motd, 0, 100);

                connection.WritePacket(answer);

                // TODO: fix this send mess
                var buffer = connection.SendBuffer.GetBuffer();
                connection.SendEvent.SetBuffer(
                    buffer, 0, (int)connection.SendBuffer.Length);

                if (!connection.Socket.SendAsync(connection.SendEvent))
                    ProcessSend(connection);

                return ReadCode.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return ReadCode.InvalidData;
            }
        }
    }
}
