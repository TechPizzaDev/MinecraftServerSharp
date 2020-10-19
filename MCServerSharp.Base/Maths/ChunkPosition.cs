using System;

namespace MCServerSharp.Maths
{
    public struct ChunkPosition : IEquatable<ChunkPosition>
    {
        public int X;
        public int Z;

        public ChunkPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public readonly bool Equals(ChunkPosition other)
        {
            return X == other.X
                && Z == other.Z;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ChunkPosition value && Equals(value);
        }

        public override readonly string ToString()
        {
            return "{X:" + X + ", Z:" + Z + "}";
        }

        public static bool operator ==(ChunkPosition left, ChunkPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkPosition left, ChunkPosition right)
        {
            return !(left == right);
        }
    }
}
