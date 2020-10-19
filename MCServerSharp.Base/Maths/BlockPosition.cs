using System;

namespace MCServerSharp.Maths
{
    public readonly struct BlockPosition : IEquatable<BlockPosition>
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public BlockPosition(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(BlockPosition other)
        {
            return X == other.X
                && Y == other.Y
                && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is BlockPosition value && Equals(value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(BlockPosition left, BlockPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockPosition left, BlockPosition right)
        {
            return !(left == right);
        }
    }
}
