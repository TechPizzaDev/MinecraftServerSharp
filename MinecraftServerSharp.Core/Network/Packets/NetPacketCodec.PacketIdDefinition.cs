using System;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCodec<TPacketID>
        where TPacketID : Enum
    {
        public readonly struct PacketIdDefinition
        {
            public Type Type { get; }
            public int RawID { get; }
            public TPacketID ID { get; }

            public PacketIdDefinition(Type type, int rawID, TPacketID id)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                RawID = rawID;
                ID = id;
            }
        }
    }
}
