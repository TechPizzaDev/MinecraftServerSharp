using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using MinecraftServerSharp.DataTypes;
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


        // TODO: move these somewhere
        public static int ProtocolVersion { get; } = 498;
        public static MinecraftVersion MinecraftVersion { get; } = new MinecraftVersion(1, 14, 4);


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

            PacketDecoder.CreateCoderDelegates();
        }

        private void SetupEncoder()
        {
            PacketEncoder.RegisterServerPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " server packet types");

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
                if (e.SocketError != SocketError.Success)
                {
                    connection.Close();
                    return;
                }

                // We process by the message length, 
                // so don't worry if we received parts of the next message.
                reader.Seek(0, SeekOrigin.End);
                connection.ReceiveBuffer.Write(e.MemoryBuffer.Span.Slice(0, e.BytesTransferred));

                if (reader.Length > 0)
                {
                    reader.Seek(0, SeekOrigin.Begin);
                    if (connection.ReceivedLength == -1)
                    {
                        if (reader.ReadByte() == 0xfe &&
                            reader.ReadByte() == 0x01)
                        {
                            bool fullyRead = ReadLegacyServerListPing(connection);
                            if (fullyRead)
                            {
                                connection.Close();
                                return;
                            }
                        }
                        else
                        {
                            reader.Seek(0, SeekOrigin.Begin);
                            if (VarInt.TryDecode(
                                reader.BaseStream,
                                out var messageLength,
                                out int messageLengthBytes))
                            {
                                connection.ReceivedLength = messageLength;
                                connection.ReceivedLengthBytes = messageLengthBytes;
                            }
                        }
                    }

                    if (connection.ReceivedLength != -1 &&
                        reader.Length >= connection.ReceivedLength)
                    {
                        int rawPacketId = connection.Reader.ReadVarInt(out int packetIdBytes);
                        if(packetIdBytes == -1)
                        {
                            connection.Kick("Packet ID is incorrectly encoded.");
                            return;
                        }

                        int packetLength = connection.ReceivedLength - packetIdBytes;
                        if (packetLength > MaxClientPacketSize)
                        {
                            connection.Kick(
                                $"Packet length {packetLength} exceeds {MaxClientPacketSize}.");
                            return;
                        }

                        if(!PacketDecoder.TryGetPacketIdDefinition(
                            connection.State, rawPacketId, out var packetIdDefinition))
                        {
                            connection.Kick($"Unknown packet ID \"{rawPacketId}\".");
                            return;
                        }

                        // TODO: do stuff with packet (and look into NetBuffer),
                        // like put it through that cool pipeline that doesn't exist yet (it almost does)
                        Console.WriteLine(
                            "(" + connection.ReceivedLengthBytes + ") " +
                            connection.ReceivedLength + ": " +
                            rawPacketId);

                        connection.TrimCurrentReceivedMessage();
                    }
                }

                if (e.BytesTransferred == 0)
                {
                    connection.Kick("Zero bytes were transferred in the operation.");
                    return;
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
                var writer = connection.Writer;

            AfterSend:
                if (e.SocketError != SocketError.Success)
                {
                    connection.Close();
                    return;
                }
                connection.TrimSendBuffer(e.BytesTransferred);

                int nextSendLength = (int)connection.SendBuffer.Length;
                if (nextSendLength == 0)
                    return;

                e.SetBuffer(connection.SendBuffer.GetBuffer(), 0, nextSendLength);
                if (!connection.Socket.SendAsync(e))
                    goto AfterSend;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Close();
            }
        }

        private bool ReadLegacyServerListPing(NetConnection connection)
        {
            // Ensure that the whole message is read,
            // as we don't know it's length.
            if (connection.Socket.Available != 0)
            {
                Thread.Sleep(5);
                return false;
            }

            try
            {
                connection.ReadPacket(out ClientLegacyServerListPing packet);

                var answer = new ServerLegacyServerListPong(
                    ProtocolVersion, MinecraftVersion, "A Minecraft Server", 0, 100);

                connection.WritePacket(answer);

                connection.SendEvent.SetBuffer(
                    connection.SendBuffer.GetBuffer(), 0, (int)connection.SendBuffer.Length);

                if (!connection.Socket.SendAsync(connection.SendEvent))
                    ProcessSend(connection);
            }
            catch
            {
            }
            return true;
        }
    }
}
