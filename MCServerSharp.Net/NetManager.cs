using System;
using System.Buffers;
using System.Collections.Generic;
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
            Listener = new NetListener(Orchestrator, AcceptConnection);

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

            Listener.Start(backlog);
        }

        private bool AcceptConnection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Add(connection))
                    throw new InvalidOperationException(); // This should never occur.
            }

            // TODO: manage connection tasks
            // TODO: validate client
            var connectionTask = Codec.EngageConnection(connection, cancellationToken: default);

            return true;
        }

        public int GetConnectionCount()
        {
            lock (ConnectionMutex)
            {
                return _connections.Count;
            }
        }

        public void UpdateConnections(List<NetConnection> connectionBuffer, out int activeConnectionCount)
        {
            if (connectionBuffer == null)
                throw new ArgumentNullException(nameof(connectionBuffer));

            lock (ConnectionMutex)
                connectionBuffer.AddRange(_connections);
            
            activeConnectionCount = 0;
            for (int i = 0; i < connectionBuffer.Count; i++)
            {
                var connection = connectionBuffer[i];
                if (connection.ProtocolState == ProtocolState.Closing)
                {
                    bool connected = connection.Socket.Connected;
                    if (!connected || connection.SendBuffer.Length == 0)
                    {
                        connection.Close(immediate: true);

                        lock (ConnectionMutex)
                        {
                            if (!_connections.Remove(connection))
                                throw new InvalidOperationException();
                        }

                        // There won't be a queue if there was no packet send attempt during connection.
                        if (Orchestrator.PacketSendQueues.TryRemove(connection, out var removedQueue))
                        {
                            foreach (var staleHolder in removedQueue.PacketQueue)
                                Orchestrator.ReturnPacketHolder(staleHolder);
                        }
                    }
                }
                else
                {
                    activeConnectionCount++;
                }
            }
        }
    }
}