using System;
using System.Diagnostics.CodeAnalysis;
using MinecraftServerSharp.Net.Packets;

namespace MinecraftServerSharp.Net
{
    public abstract class PacketHolder
    {
        public long TransactionId { get; set; }
        public NetConnection? TargetConnection { get; set; }

        public abstract Type PacketType { get; }
    }

    public class PacketHolder<TPacket> : PacketHolder
    {
        public NetPacketEncoder.PacketWriterDelegate<TPacket> WriterDelegate { get; }
        public ProtocolState State { get; set; }

        [AllowNull]
        public TPacket Packet { get; set; }

        public override Type PacketType => typeof(TPacket);

        public PacketHolder(NetPacketEncoder.PacketWriterDelegate<TPacket> writerDelegate)
        {
            WriterDelegate = writerDelegate ?? throw new ArgumentNullException(nameof(writerDelegate));
        }
    }
}
