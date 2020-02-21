using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace MinecraftServerSharp.Network
{
    public class NetOrchestrator
    {
        private List<NetOrchestratorWorker> _workers;

        public NetProcessor Processor { get; }

        public ConcurrentQueue<PacketHolder> PacketSendQueue { get; }

        public NetOrchestrator(NetProcessor processor)
        {
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));

            PacketSendQueue = new ConcurrentQueue<PacketHolder>();

            _workers = new List<NetOrchestratorWorker>();
        }

        public void Start(int workerCount)
        {
            workerCount = Math.Min(workerCount, Environment.ProcessorCount * 2);

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
            {
                worker.Stop();
            }
            _workers.Clear();
        }

        public void Flush()
        {
            foreach (var worker in _workers)
                worker.Flush();

            foreach (var worker in _workers)
                worker.AwaitFlush();
        }

        public PacketHolder<TPacket> EnqueuePacket<TPacket>(NetConnection target, TPacket packet)
        {
            // TODO: pool packet holders
            var writerDelegate = Processor.PacketEncoder.GetPacketWriter<TPacket>();
            var holder = new PacketHolder<TPacket>(writerDelegate);

            holder.TargetConnection = target;
            holder.Packet = packet;

            PacketSendQueue.Enqueue(holder);

            return holder;
        }
    }
}
