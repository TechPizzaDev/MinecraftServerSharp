using System;

namespace MCServerSharp.Maths
{
    public readonly struct ChunkRegionPosition : IEquatable<ChunkRegionPosition>, ILongHashable
    {
        public int X { get; }
        public int Z { get; }

        public ChunkRegionPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public ChunkRegionPosition(ChunkColumnPosition columnPosition) : this(columnPosition.X >> 5, columnPosition.Z >> 5)
        {
        }

        public bool Equals(ChunkRegionPosition other)
        {
            return this == other;
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(X, Z);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public override bool Equals(object? obj)
        {
            return obj is ChunkRegionPosition value && Equals(value);
        }

        public override string ToString()
        {
            return "{X:" + X + ", Z:" + Z + "}";
        }

        public static bool operator ==(ChunkRegionPosition a, ChunkRegionPosition b)
        {
            return a.X == b.X
                && a.Z == b.Z;
        }

        public static bool operator !=(ChunkRegionPosition a, ChunkRegionPosition b)
        {
            return !(a == b);
        }
    }

}
