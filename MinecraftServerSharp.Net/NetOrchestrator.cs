using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    public class NetOrchestratorQueue
    {
        public NetConnection Connection { get; }

        public object EngageMutex { get; } = new object();
        public ConcurrentQueue<PacketHolder> SendQueue { get; } = new ConcurrentQueue<PacketHolder>();

        public bool IsEngaged { get; set; }

        public NetOrchestratorQueue(NetConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }

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
        //private int _workerHeuristicSequenceIndex;
        //private int _workerHeuristicSequenceOverflow;

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketCodec Codec { get; }

        public ConcurrentDictionary<NetConnection, NetOrchestratorQueue> PacketSendQueues { get; } =
            new ConcurrentDictionary<NetConnection, NetOrchestratorQueue>();

        /// <summary>
        /// Holds queues that have packets to send.
        /// </summary>
        public ConcurrentQueue<NetOrchestratorQueue> QueuesToFlush { get; } =
            new ConcurrentQueue<NetOrchestratorQueue>();

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
            Console.WriteLine($"Started {_workers.Count} {nameof(NetOrchestrator)} workers");
        }

        public void Stop()
        {
            foreach (var worker in _workers)
                worker.Stop();

            _workers.Clear();
        }

        public void RequestFlush()
        {
            // TODO: smarter flushing

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

            var encoder = Codec.Encoder;
            var writer = encoder.GetPacketWriter<TPacket>();

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
            if (packetHolder.Connection == null)
                throw new ArgumentException("No assigned connection.");

            if (!PacketSendQueues.TryGetValue(packetHolder.Connection, out var queue))
            {
                queue = new NetOrchestratorQueue(packetHolder.Connection);
                PacketSendQueues.TryAdd(queue.Connection, queue);
            }
            queue.SendQueue.Enqueue(packetHolder);

            if (queue.IsEngaged)
                return;

            bool requestFlush = false;
            lock (queue.EngageMutex)
            {
                if (queue.IsEngaged)
                    return;

                requestFlush = true;
                queue.IsEngaged = true;
                QueuesToFlush.Enqueue(queue);
            }

            if (requestFlush)
                RequestFlush();
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

        //public NetOrchestratorWorker GetWorker(WorkerPickingHeuristic pickingHeuristic)
        //{
        //    switch (pickingHeuristic)
        //    {
        //        case WorkerPickingHeuristic.Sequence:
        //            int maxIndex = _workers.Count;
        //            int workerIndex = Interlocked.Increment(ref _workerHeuristicSequenceIndex);
        //            if (_workerHeuristicSequenceIndex >= maxIndex)
        //            {
        //                _workerHeuristicSequenceIndex = -1;
        //                _workerHeuristicSequenceOverflow++;
        //            }
        //
        //            // This should be a rare occurence.
        //            if (workerIndex >= maxIndex)
        //            {
        //                _workerHeuristicSequenceOverflow %= maxIndex;
        //                workerIndex = (workerIndex + _workerHeuristicSequenceOverflow) % maxIndex;
        //            }
        //
        //            return _workers[workerIndex];
        //
        //        case WorkerPickingHeuristic.LeastActive:
        //            int queuedPackets = int.MaxValue;
        //            NetOrchestratorWorker? worker = null;
        //            for (int i = 0; i < _workers.Count; i++)
        //            {
        //                int queuedOfWorker = _workers[i].PacketSendQueueCount;
        //                if (queuedOfWorker < queuedPackets)
        //                {
        //                    worker = _workers[i];
        //                    queuedPackets = queuedOfWorker;
        //                }
        //            }
        //
        //            Debug.Assert(worker != null);
        //            return worker;
        //
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(pickingHeuristic));
        //    }
        //}

        public enum WorkerPickingHeuristic
        {
            Sequence,
            LeastActive
        }
    }
}

