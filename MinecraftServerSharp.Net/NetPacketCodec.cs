using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    public delegate void PacketHandlerDelegate(
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

            Decoder.InitializePacketIdMaps();

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

            Encoder.InitializePacketIdMaps();

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

        public void AddConnection(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            connection.ReceiveEvent.Completed += (s, e) => ProcessReceive((NetConnection)e.UserToken);
            connection.SendEvent.Completed += (s, e) => ProcessSend((NetConnection)e.UserToken);

            new Thread(() =>
            {
                // TODO: fix this issue please
                //       possible have to drop SocketAsyncEventArgs in favor of non-blocking Sockets

                Thread.Sleep(10000);
                if (connection.State == ProtocolState.Status)
                {
                    if (connection.Socket.ReceiveAsync(connection.ReceiveEvent))
                    {
                        Console.WriteLine(connection.ReceiveEvent.BytesTransferred);
                    }
                }
            }).Start();

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
                var reader = connection.BufferReader;

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

                        if (!PacketHandlers.TryGetValue(packetIdDefinition.Id, out var packetHandler))
                            throw new Exception($"Missing packet handler for \"{packetIdDefinition.Id}\".");

                        packetHandler.Invoke(connection, rawPacketId, packetIdDefinition);

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
                connection.Kick("Server Error: \n" + ex.ToString().Replace("\r", ""));
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

                TryFlushSendBuffer(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connection.Close(immediate: true);
            }
        }

        public void TryFlushSendBuffer(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            lock (connection.WriteMutex)
            {
                long length = connection.SendBuffer.Length;
                if (length > 0 && connection.State != ProtocolState.Disconnected)
                {
                    var buffer = connection.SendBuffer.GetBlock(0);
                    int blockLength = Math.Min(connection.SendBuffer.BlockSize, (int)length);
                    connection.SendEvent.SetBuffer(buffer.Slice(0, blockLength));

                    if (!connection.Socket.SendAsync(connection.SendEvent))
                        ProcessSend(connection);
                }
            }
        }

        private bool ValidatePacketAndGetId(
            NetConnection connection,
            out VarInt rawPacketId,
            out NetPacketDecoder.PacketIdDefinition definition)
        {
            if (connection.BufferReader.Read(
                out rawPacketId, out int packetIdBytes) != OperationStatus.Done)
            {
                connection.Kick("Packet ID is incorrectly encoded.");
                definition = default;
                return false;
            }

            int packetLength = connection.ReceivedLength - packetIdBytes;
            if (packetLength > NetManager.MaxClientPacketSize)
            {
                connection.Kick(
                    $"Packet length {packetLength} exceeds {NetManager.MaxClientPacketSize}.");
                definition = default;
                return false;
            }

            if (!Decoder.TryGetPacketIdDefinition(
                connection.State, rawPacketId, out definition))
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
