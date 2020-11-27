using System;
using System.Numerics;

namespace MCServerSharp.Maths
{
    public struct Vector3d : IEquatable<Vector3d>, ILongHashable
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly Vector3 ToVector3()
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }

        public readonly bool Equals(Vector3d other)
        {
            return this == other;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Vector3d value && Equals(value);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public readonly long GetLongHashCode()
        {
            return LongHashCode.Combine(X, Y, Z);
        }

        public override readonly string ToString()
        {
            return "<" + X + "  " + Y + "  " + Z + ">";
        }

        public static bool operator ==(Vector3d a, Vector3d b)
        {
            return a.X == b.X
                && a.Y == b.Y
                && a.Z == b.Z;
        }

        public static bool operator !=(Vector3d a, Vector3d b)
        {
            return !(a == b);
        }
    }
}
