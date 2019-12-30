using System;
using System.IO;
using System.Net.Sockets;
using MinecraftServerSharp.DataTypes;
using MinecraftServerSharp.Network.Packets;

namespace MinecraftServerSharp.Network
{
    public partial class NetProcessor
    {
        public NetPacketDecoder PacketDecoder { get; }
        public NetPacketEncoder PacketEncoder { get; }

        public NetProcessor()
        {
            PacketDecoder = new NetPacketDecoder();
            PacketEncoder = new NetPacketEncoder();
        }

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

        public void AddConnection(NetConnection connection)
        {
            connection.SocketEvent.Completed += ConnectionSocketEvent_Completed;

            // As soon as the client is connected, post a receive to the connection
            if (!connection.Socket.ReceiveAsync(connection.SocketEvent))
                ProcessReceive(connection);
        }

        private void ConnectionSocketEvent_Completed(object s, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive((NetConnection)e.UserToken);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend((NetConnection)e.UserToken);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive or send.");
            }
        }

        private void ProcessReceive(NetConnection connection)
        {
            // TODO: this only reads uncompressed packets for now, 
            // this will require slight change when compressed packets are implemented

            try
            {
                var e = connection.SocketEvent;
                var reader = connection.Reader;

            AfterReceive:
                if (e.BytesTransferred > 0 &&
                    e.SocketError == SocketError.Success)
                {
                    // We process by the message length, 
                    // so don't worry if we received parts of the next message.
                    reader.Seek(0, SeekOrigin.End);
                    connection.ReadBuffer.Write(e.MemoryBuffer.Span.Slice(0, e.BytesTransferred));

                TryRead:
                    reader.Seek(0, SeekOrigin.Begin);
                    if (connection.MessageLength == -1)
                    {
                        if (reader.ReadByte() == 0xfe &&
                            reader.ReadByte() == 0x01)
                        {
                            int length = ReadLegacyServerListPing(connection);
                            connection.TrimMessageBuffer(length);
                            goto TryRead;
                        }
                        else
                        {
                            reader.Seek(0, SeekOrigin.Begin);
                        }

                        if (VarInt32.TryDecode(
                            reader.BaseStream,
                            out VarInt32 messageLength,
                            out int messageLengthBytes))
                        {
                            connection.MessageLength = messageLength;
                            connection.MessageLengthBytes = messageLengthBytes;
                        }
                        else
                        {
                            goto ReceiveNext;
                        }
                    }

                    if (reader.Length >= connection.MessageLength)
                    {
                        int rawPacketID = connection.Reader.ReadVarInt32();

                        // TODO: do stuff with packet (and look into NetBuffer),
                        // like put it through that cool pipeline that doesn't exist yet (it almost does)
                        Console.WriteLine(
                            "(" + connection.MessageLengthBytes + ") " +
                            connection.MessageLength + ": " +
                            rawPacketID);

                        connection.TrimCurrentMessage();
                    }

                ReceiveNext:
                    if (!connection.Socket.ReceiveAsync(e))
                        goto AfterReceive;
                }
                else
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(nameof(ProcessReceive) + ": " + ex);
                connection.Close();
            }
        }

        private void ProcessSend(NetConnection connection)
        {
            try
            {
                var e = connection.SocketEvent;
                var msgBuffer = connection.ReadBuffer;

                if (e.SocketError == SocketError.Success)
                {

                }
                else
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(nameof(ProcessReceive) + ": " + ex);
                connection.Close();
            }
        }

        private int ReadLegacyServerListPing(NetConnection connection)
        {
            var packet = connection.ReadPacket<ClientLegacyServerListPing>();

            var answer = new ServerLegacyServerListPing();
            answer.Write(connection.Writer);
            throw new NotImplementedException("LEGACY PING");
        }
    }
}
