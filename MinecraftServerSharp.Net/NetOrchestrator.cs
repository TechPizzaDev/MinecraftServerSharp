using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    /// <summary>
    /// Orchestrates threads through <see cref="NetOrchestratorWorker"/> instances.
    /// </summary>
    public class NetOrchestrator
    {
        private List<NetOrchestratorWorker> _workers;
        private int _workerHeuristicSequenceIndex;
        private int _workerHeuristicSequenceOverflow;

        public RecyclableMemoryManager MemoryManager { get; }
        public NetPacketCodec Codec { get; }

        public NetOrchestrator(RecyclableMemoryManager memoryManager, NetPacketCodec codec)
        {
            MemoryManager = memoryManager ??
                throw new ArgumentNullException(nameof(memoryManager));

            Codec = codec ?? throw new ArgumentNullException(nameof(codec));

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
            foreach (var worker in _workers)
                worker.RequestFlush();
        }

        public void EnqueuePacket<TPacket>(NetConnection target, TPacket packet)
        {
            var worker = GetWorker(WorkerPickingHeuristic.LeastActive);
            var packetHolder = worker.GetPacketHolder(packet, target);
            worker.EnqueuePacket(packetHolder);
        }

        public NetOrchestratorWorker GetWorker(WorkerPickingHeuristic pickingHeuristic)
        {
            switch (pickingHeuristic)
            {
                case WorkerPickingHeuristic.Sequence:
                    int maxIndex = _workers.Count;
                    int workerIndex = Interlocked.Increment(ref _workerHeuristicSequenceIndex);
                    if (_workerHeuristicSequenceIndex >= maxIndex)
                    {
                        _workerHeuristicSequenceIndex = -1;
                        _workerHeuristicSequenceOverflow++;
                    }

                    // This should be a rare occurence.
                    if (workerIndex >= maxIndex)
                    {
                        _workerHeuristicSequenceOverflow %= maxIndex;
                        workerIndex = (workerIndex + _workerHeuristicSequenceOverflow) % maxIndex;
                    }

                    return _workers[workerIndex];

                case WorkerPickingHeuristic.LeastActive:
                    int queuedPackets = int.MaxValue;
                    NetOrchestratorWorker? worker = null;
                    for (int i = 0; i < _workers.Count; i++)
                    {
                        int queuedOfWorker = _workers[i].PacketSendQueueCount;
                        if (queuedOfWorker < queuedPackets)
                        {
                            worker = _workers[i];
                            queuedPackets = queuedOfWorker;
                        }
                    }

                    Debug.Assert(worker != null);
                    return worker;

                default:
                    throw new ArgumentOutOfRangeException(nameof(pickingHeuristic));
            }
        }

        public enum WorkerPickingHeuristic
        {
            Sequence,
            LeastActive
        }
    }
}

