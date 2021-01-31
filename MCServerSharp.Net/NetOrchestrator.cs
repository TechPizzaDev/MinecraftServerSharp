using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;

namespace MCServerSharp.Net
{
    /// <summary>
    /// Orchestrates threads through <see cref="NetOrchestratorWorker"/> instances.
    /// </summary>
    public class NetOrchestrator
    {
        public const int PacketPoolItemLimit = 64;
        public const int PacketPoolCommonItemLimit = 256;

        private static HashSet<Type> _commonPacketTypes = new HashSet<Type>
        {
            typeof(ServerKeepAlive),
            typeof(ClientKeepAlive),
            typeof(ClientPlayerPosition),
            typeof(ClientPlayerRotation),
            typeof(ClientPlayerPositionRotation)
        };

        private PacketHolderPool _packetHolderPool;
        private List<NetOrchestratorWorker> _workers;

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketCodec Codec { get; }

        public ConcurrentDictionary<NetConnection, NetPacketSendQueue> PacketSendQueues { get; } =
            new ConcurrentDictionary<NetConnection, NetPacketSendQueue>();

        public NetOrchestrator(RecyclableMemoryManager memoryManager, NetPacketCodec codec)
        {
            MemoryManager = memoryManager ??
                throw new ArgumentNullException(nameof(memoryManager));

            Codec = codec ?? throw new ArgumentNullException(nameof(codec));

            _packetHolderPool = new PacketHolderPool(StorePacketPredicate);
            _workers = new List<NetOrchestratorWorker>();
        }

        public void Start(int workerCount)
        {
            if (workerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(workerCount));

            for (int i = 0; i < workerCount; i++)
            {
                var worker = new NetOrchestratorWorker(this);
                worker.Thread.Name = $"{nameof(NetOrchestrator)} {i + 1}";
                worker.Start();

                _workers.Add(worker);
            }
        }

        public void Stop()
        {
            foreach (var worker in _workers)
                worker.Stop();

            _workers.Clear();
        }

        public void RequestFlush()
        {
            foreach (var worker in _workers)
                worker.RequestFlush();
        }

        public void ReturnPacketHolder(PacketHolder packetHolder)
        {
            lock (_packetHolderPool)
                _packetHolderPool.Return(packetHolder);
        }

        public PacketHolder<TPacket> RentPacketHolder<TPacket>(
            NetConnection connection, in TPacket packet)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            var writer = Codec.Encoder.GetPacketWriterAction<TPacket>();

            lock (_packetHolderPool)
            {
                return _packetHolderPool.Rent(
                    writer,
                    connection,
                    packet);
            }
        }

        public void EnqueuePacket(PacketHolder packetHolder)
        {
            if (packetHolder == null)
                throw new ArgumentNullException(nameof(packetHolder));

            NetConnection? holderConnection = packetHolder.Connection;
            if (holderConnection == null)
                throw new ArgumentException("No assigned connection.");

            if (!PacketSendQueues.TryGetValue(holderConnection, out NetPacketSendQueue? sendQueue))
            {
                if (!holderConnection.IsAlive)
                    throw new ArgumentException("The assigned connection is not alive.");

                sendQueue = new NetPacketSendQueue(holderConnection);
                PacketSendQueues.TryAdd(sendQueue.Connection, sendQueue);
            }

            sendQueue.Packets.Enqueue(packetHolder);

            EnqueueQueue(sendQueue);
        }

        public void EnqueueQueue(NetPacketSendQueue sendQueue)
        {
            lock (sendQueue.EngageMutex)
            {
                if (sendQueue.IsEngaged)
                    return;
                sendQueue.IsEngaged = true;
            }

            NetOrchestratorWorker worker = GetWorker();
            worker.Enqueue(sendQueue);
            worker.RequestFlush();
        }

        private NetOrchestratorWorker GetWorker()
        {
            // TODO: 
            return _workers[0];
        }

        public void EnqueuePacket<TPacket>(
            NetConnection target, in TPacket packet)
        {
            var packetHolder = RentPacketHolder(target, packet);
            EnqueuePacket(packetHolder);
        }

        private static bool StorePacketPredicate(
            PacketHolderPool sender, Type packetType, int currentCount)
        {
            int limit = PacketPoolItemLimit;

            if (_commonPacketTypes.Contains(packetType))
                limit = PacketPoolCommonItemLimit;

            return currentCount < limit;
        }
    }
}

