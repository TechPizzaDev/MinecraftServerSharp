using System;
using System.Collections.Generic;

namespace MinecraftServerSharp.Net.Packets
{
    public abstract partial class NetPacketCodec<TPacketId>
        where TPacketId : Enum
    {
        public readonly struct PacketIdDefinition : IEquatable<PacketIdDefinition>
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

            public bool Equals(PacketIdDefinition other)
            {
                return Type == other.Type
                    && RawId == other.RawId
                    && EqualityComparer<TPacketId>.Default.Equals(Id, other.Id);
            }

            public override bool Equals(object? obj)
            {
                return obj is PacketIdDefinition other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Type, RawId, Id);
            }

            public static bool operator ==(PacketIdDefinition left, PacketIdDefinition right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(PacketIdDefinition left, PacketIdDefinition right)
            {
                return !(left == right);
            }
        }
    }
}
