using System;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    public abstract class PacketHolder
    {
        // TODO: Add ReferenceCount to allow multiple workers to send concurrently
        //       and List<NetConnection> to allow sending one packet to multiple clients.
        //       The worker that decrements ReferenceCount to 0 returns the PacketHolder.

        public NetConnection? Connection { get; set; }
        public ProtocolState State { get; set; }
        public int CompressionThreshold { get; set; }

        public abstract Type PacketType { get; }
    }

    public class PacketHolder<TPacket> : PacketHolder
    {
        public NetPacketWriterAction<TPacket> Writer { get; set; }
        
        [AllowNull]
        public TPacket Packet { get; set; }

        public override Type PacketType => typeof(TPacket);

        public PacketHolder()
        {
            Writer = default!;
            Packet = default;
        }
    }
}
