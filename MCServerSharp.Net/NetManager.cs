using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using MCServerSharp.Collections;
using MCServerSharp.Data.IO;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    public class NetManager
    {

        public const int BlockSize = 1024 * 16;
        public const int BlockMultiple = BlockSize * 16;
        public const int MaxBufferSize = BlockMultiple * 16;

        // These fit pretty well with the memory block sizes.
        public const int MaxServerPacketSize = 2097152;
        public const int MaxClientPacketSize = 32768;

        // TODO: move these somewhere
        public int ProtocolVersion { get; } = 578;
        public MCVersion GameVersion { get; } = new MCVersion(1, 15, 2);
        public bool Config_AppendGameVersionToBetaStatus { get; } = true;

        private HashSet<NetConnection> _connections;

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketCodec Codec { get; }
        public NetOrchestrator Orchestrator { get; }
        public NetListener Listener { get; }

        public object ConnectionMutex { get; } = new object();
        public ReadOnlySet<NetConnection> Connections { get; }

        public NetManager(RecyclableMemoryManager memoryManager)
        {
            MemoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            Codec = new NetPacketCodec(MemoryManager);
            Orchestrator = new NetOrchestrator(MemoryManager, Codec);
            Listener = new NetListener(Orchestrator);

            _connections = new HashSet<NetConnection>();
            Connections = _connections.AsReadOnly();
        }

        public NetManager(int blockSize, int blockMultiple, int maxBufferSize) :
            this(CreateManager(blockSize, blockMultiple, maxBufferSize))
        {
        }

        public NetManager() : this(BlockSize, BlockMultiple, MaxBufferSize)
        {
        }

        private static RecyclableMemoryManager CreateManager(
            int blockSize, int blockMultiple, int maxBufferSize)
        {
            if (maxBufferSize < Math.Max(MaxClientPacketSize, MaxServerPacketSize))
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

            return new RecyclableMemoryManager(blockSize, blockMultiple, maxBufferSize);
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            Listener.Bind(localEndPoint);
        }

        public void Setup()
        {
            Codec.SetupCoders();
        }

        public void SetPacketHandler<TPacket>(ClientPacketId id, Action<NetConnection, TPacket> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Codec.SetPacketHandler(id, delegate (
                NetConnection connection,
                NetBinaryReader packetReader,
                NetPacketDecoder.PacketIdDefinition packetIdDefinition,
                out int messageLength)
            {
                var status = connection.ReadPacket<TPacket>(packetReader, out var packet, out messageLength);
                if (status != OperationStatus.Done)
                    return status;

                handler.Invoke(connection, packet);
                return OperationStatus.Done;
            });
        }

        public void SetPacketHandler<TPacket>(Action<NetConnection, TPacket> handler)
        {
            var packetStruct = typeof(TPacket).GetCustomAttribute<PacketStructAttribute>();
            if (packetStruct == null)
                throw new ArgumentException($"The type is missing a \"{nameof(PacketStructAttribute)}\".");

            if (!packetStruct.IsClientPacket)
                throw new ArgumentException("The packet is not a client packet.");

            SetPacketHandler((ClientPacketId)packetStruct.PacketId, handler);
        }

        public void Listen(int backlog)
        {
            Orchestrator.Start(workerCount: 4);

            Listener.Connection += Listener_Connection;
            Listener.Disconnection += Listener_Disconnection;

            Listener.Start(backlog);
        }

        private void Listener_Connection(NetListener sender, NetConnection connection)
        {   
            lock (ConnectionMutex)
            {
                if (!_connections.Add(connection))
                    throw new InvalidOperationException();
            }

            // TODO: manage connection tasks
            var connectionTask = Codec.EngageConnection(connection, cancellationToken: default);
        }

        private void Listener_Disconnection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Remove(connection))
                    throw new InvalidOperationException();
            }
        }

        public int GetConnectionCount()
        {
            lock (ConnectionMutex)
            {
                return _connections.Count;
            }
        }

        public int UpdateConnections(out int activeConnectionCount)
        {
            var list = new List<NetConnection>(_connections.Count);
            lock (ConnectionMutex)
            {
                list.AddRange(_connections);
            }

            int activeCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var connection = list[i];
                if (connection.ProtocolState == ProtocolState.Closing)
                {
                    if (Orchestrator.PacketSendQueues.TryGetValue(connection, out var queue) &&
                        !queue.IsEngaged)
                    {
                        if (connection.SendBuffer.Length == 0 || !connection.Socket.Connected)
                            connection.Close(immediate: true);
                        else
                            Console.WriteLine("Delaying close");
                    }
                }
                else
                {
                    activeCount++;
                }
            }

            activeConnectionCount = activeCount;
            return list.Count;
        }

        public void TickAlive(long keepAliveId)
        {
            lock (ConnectionMutex)
            {
                foreach (NetConnection connection in Connections)
                {
                    if (connection.ProtocolState == ProtocolState.Play)
                        connection.EnqueuePacket(new ServerKeepAlive(keepAliveId));
                }
            }
        }
    }
}