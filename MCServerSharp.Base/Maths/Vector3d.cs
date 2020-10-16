using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MCServerSharp.Maths
{
    public struct Vector3d : IEquatable<Vector3d>
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
            return X == other.X
                && Y == other.Y
                && Z == other.Z;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Vector3d value && Equals(value);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override readonly string ToString()
        {
            return "<" + X + "  " + Y + "  " + Z + ">";
        }
    }
}
