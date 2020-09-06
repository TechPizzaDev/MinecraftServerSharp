using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using MinecraftServerSharp.Collections;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
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
        public MinecraftVersion GameVersion { get; } = new MinecraftVersion(1, 15, 2);
        public bool Config_AppendGameVersionToBetaStatus { get; } = true;

        private HashSet<NetConnection> _connections;
        private string? _requestPongBase;

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

            Codec.SetPacketHandler(id, (connection, rawId, definition) =>
            {
                var (status, length) = connection.ReadPacket<TPacket>(out var packet);
                if (status == OperationStatus.Done)
                {
                    handler.Invoke(connection, packet);
                }
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
            // TODO: fix some kind of concurrency that corrupts sent data
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

            Codec.AddConnection(connection);
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

        public void TickAlive(long keepAliveId)
        {
            lock (ConnectionMutex)
            {
                foreach (NetConnection connection in Connections)
                {
                    connection.EnqueuePacket(new ServerKeepAlive(keepAliveId));
                }
            }
        }
    }
}