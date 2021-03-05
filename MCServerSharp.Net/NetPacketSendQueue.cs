using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Net
{
    /// <summary>
    /// Used to hold a thread-safe queue of packets to a client.
    /// </summary>
    public class NetPacketSendQueue
    {
        private ConcurrentQueue<PacketHolder> _packets = new();

        public NetConnection Connection { get; }

        public object PacketMutex { get; } = new object();
        public object EngageMutex { get; } = new object();

        /// <summary>
        /// Gets whether a <see cref="NetOrchestratorWorker"/> is processing this queue.
        /// </summary>
        public bool IsEngaged { get; set; }

        public bool IsEmpty => _packets.IsEmpty;

        public NetPacketSendQueue(NetConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Enqueue(PacketHolder packetHolder)
        {
            Debug.Assert(packetHolder != null);

            _packets.Enqueue(packetHolder);
        }

        public bool TryPeek([MaybeNullWhen(false)] out PacketHolder packetHolder)
        {
            return _packets.TryPeek(out packetHolder);
        }

        public bool TryDequeue([MaybeNullWhen(false)] out PacketHolder packetHolder)
        {
            return _packets.TryDequeue(out packetHolder);
        }
    }
}

