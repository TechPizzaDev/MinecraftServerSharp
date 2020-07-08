using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
            workerCount = Math.Min(workerCount, Environment.ProcessorCount);

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

        public void Flush()
        {
            foreach (var worker in _workers)
                worker.RequestFlush();
        }

        public PacketHolder<TPacket> GetPacketHolder<TPacket>(NetConnection target, TPacket packet)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            // TODO: pool packet holders
            var writerDelegate = Processor.PacketEncoder.GetPacketWriter<TPacket>();
            var holder = new PacketHolder<TPacket>(writerDelegate);

            holder.State = target.State;
            holder.TargetConnection = target;
            holder.Packet = packet;

            return holder;
        }

        public void EnqueuePacket(PacketHolder packetHolder)
        {
            PacketSendQueue.Enqueue(packetHolder);

            // TODO: get a heuristic of which worker would be most likely to flush packet
            foreach (var worker in _workers)
                worker.RequestFlush();
        }

        public void EnqueuePacket<TPacket>(NetConnection target, TPacket packet)
        {
            var packetHolder = GetPacketHolder(target, packet);
            EnqueuePacket(packetHolder);
        }
    }
}
