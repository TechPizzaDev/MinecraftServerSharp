using System;
using System.Diagnostics.CodeAnalysis;
using MinecraftServerSharp.Net.Packets;

namespace MinecraftServerSharp.Net
{
    public abstract class PacketHolder
    {
        public long TransactionId { get; set; }
        public NetConnection? Connection { get; set; }

        public abstract Type PacketType { get; }
    }

    public class PacketHolder<TPacket> : PacketHolder
    {
        public NetPacketWriterDelegate<TPacket> Writer { get; set; }
        public ProtocolState State { get; set; }

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
