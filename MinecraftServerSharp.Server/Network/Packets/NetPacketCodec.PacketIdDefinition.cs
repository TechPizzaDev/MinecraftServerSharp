using System;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCodec<TPacketId>
        where TPacketId : Enum
    {
        public readonly struct PacketIdDefinition
        {
            public Type Type { get; }
            public int RawId { get; }
            public TPacketId Id { get; }

            public PacketIdDefinition(Type type, int rawId, TPacketId id)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                RawId = rawId;
                Id = id;
            }
        }
    }
}
