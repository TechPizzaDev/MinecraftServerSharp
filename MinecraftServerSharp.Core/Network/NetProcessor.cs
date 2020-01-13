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
        public const int BlockMultiple = BlockSize * 4;
        public const int MaxPacketSize = BlockMultiple * 32; // 2097152

        // TODO: move these somewhere
        public static int ProtocolVersion { get; } = 498;
        public static MinecraftVersion MinecraftVersion { get; } = new MinecraftVersion(1, 14, 4);

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketDecoder PacketDecoder { get; }
        public NetPacketEncoder PacketEncoder { get; }

        #region Constructors

        public NetProcessor(int blockSize, int blockMultiple, int maxPacketSize)
        {
            MemoryManager = new RecyclableMemoryManager(blockSize, blockMultiple, maxPacketSize);
            PacketDecoder = new NetPacketDecoder();
            PacketEncoder = new NetPacketEncoder();
        }

        public NetProcessor() : this(BlockSize, BlockMultiple, MaxPacketSize)
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

            PacketDecoder.PreparePacketTypes();
        }

        private void SetupEncoder()
        {
            PacketEncoder.RegisterServerPacketTypesFromCallingAssembly();
            Console.WriteLine("Registered " + PacketDecoder.RegisteredTypeCount + " server packet types");

            PacketEncoder.PreparePacketTypes();
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
                            if (VarInt32.TryDecode(
                                reader.BaseStream,
                                out VarInt32 messageLength,
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
                        int rawPacketID = connection.Reader.ReadVarInt();

                        // TODO: do stuff with packet (and look into NetBuffer),
                        // like put it through that cool pipeline that doesn't exist yet (it almost does)
                        Console.WriteLine(
                            "(" + connection.ReceivedLengthBytes + ") " +
                            connection.ReceivedLength + ": " +
                            rawPacketID);

                        connection.TrimCurrentReceivedMessage();
                    }
                }

                if (e.BytesTransferred == 0)
                {
                    connection.Close();
                    return;
                }

                if (!connection.Socket.ReceiveAsync(e))
                    goto AfterReceive;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Close();
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
