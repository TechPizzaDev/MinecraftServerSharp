﻿using System;

namespace MCServerSharp.Maths
{
    public readonly struct ChunkColumnPosition : IEquatable<ChunkColumnPosition>, ILongHashable
    {
        public int X { get; }
        public int Z { get; }

        public ChunkColumnPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static double Dot(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            return (a.X * b.X) + (a.Z * b.Z);
        }

        public static double DistanceSquared(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            var difference = a - b;
            return Dot(difference, difference);
        }

        public static ChunkColumnPosition operator +(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            return new ChunkColumnPosition(
                a.X + b.X,
                a.Z + b.Z);
        }

        public static ChunkColumnPosition operator -(ChunkColumnPosition left, ChunkColumnPosition right)
        {
            return new ChunkColumnPosition(
                left.X - right.X,
                left.Z - right.Z);
        }

        public bool Equals(ChunkColumnPosition other)
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
            return obj is ChunkColumnPosition value && Equals(value);
        }

        public override string ToString()
        {
            return $"{{X:{X}, Z:{Z}}}";
        }

        public static bool operator ==(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            return a.X == b.X
                && a.Z == b.Z;
        }

        public static bool operator !=(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            return !(a == b);
        }
    }
}
