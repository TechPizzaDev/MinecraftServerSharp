using System;

namespace MCServerSharp.Maths
{
    public struct ChunkPosition : IEquatable<ChunkPosition>, ILongHashable
    {
        public int X;
        public int Y;
        public int Z;

        public readonly ChunkColumnPosition ColumnPosition => new ChunkColumnPosition(X, Z);

        public ChunkPosition(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static double Dot(ChunkPosition a, ChunkPosition b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        public static double DistanceSquared(ChunkPosition a, ChunkPosition b)
        {
            var difference = a - b;
            return Dot(difference, difference);
        }

        public static ChunkPosition operator +(ChunkPosition a, ChunkPosition b)
        {
            return new ChunkPosition(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z);
        }

        public static ChunkPosition operator -(ChunkPosition left, ChunkPosition right)
        {
            return new ChunkPosition(
                left.X - right.X,
                left.Y - right.Y,
                left.Z - right.Z);
        }

        public readonly bool Equals(ChunkPosition other)
        {
            return this == other;
        }

        public readonly long GetLongHashCode()
        {
            return LongHashCode.Combine(X, Y, Z);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ChunkPosition value && Equals(value);
        }

        public override readonly string ToString()
        {
            return "{X:" + X + ", Y:" + Y + ", Z:" + Z + "}";
        }

        public static bool operator ==(ChunkPosition a, ChunkPosition b)
        {
            return a.X == b.X
                && a.Y == b.Y
                && a.Z == b.Z;
        }

        public static bool operator !=(ChunkPosition a, ChunkPosition b)
        {
            return !(a == b);
        }
    }
}
