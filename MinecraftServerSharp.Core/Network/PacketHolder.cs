using System;
using MinecraftServerSharp.Network.Packets;

namespace MinecraftServerSharp.Network
{
    public abstract class PacketHolder
    {
        public long TransactionID { get; internal set; }
        public NetConnection TargetConnection { get; internal set; }

        public abstract Type PacketType { get; }
    }

    public class PacketHolder<TPacket> : PacketHolder
    {
        public NetPacketEncoder.PacketWriterDelegate<TPacket> WriterDelegate { get; }
        public TPacket Packet { get; internal set; }

        public override Type PacketType => typeof(TPacket);

        public PacketHolder(NetPacketEncoder.PacketWriterDelegate<TPacket> writerDelegate)
        {
            WriterDelegate = writerDelegate ?? throw new ArgumentNullException(nameof(writerDelegate));
        }
    }
}
