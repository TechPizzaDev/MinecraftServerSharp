using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MCServerSharp.Utility;

namespace MCServerSharp
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct DegreeLook : IEquatable<DegreeLook>, IEquatable<Look>
    {
        public float Yaw;
        public float Pitch;

        public readonly float RadYaw => Yaw * MathF.PI / 180;
        public readonly float RadPitch => Pitch * MathF.PI / 180;

        public DegreeLook(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public static DegreeLook FromVectors(Vector3 origin, Vector3 towards)
        {
            Vector3 d = towards - origin;
            float r = d.Length();

            float yaw = -MathF.Atan2(d.X, d.Z) / MathF.PI * 180;
            if (yaw < 0)
                yaw = 360 + yaw;

            float pitch = -MathF.Asin(d.Y / r) / MathF.PI * 180;

            return new DegreeLook(yaw, pitch);
        }

        public readonly Vector3 ToUnitVector3()
        {
            float p = RadYaw;
            float y = RadPitch;

            return new Vector3(
                x: -MathF.Cos(p) * MathF.Sin(y),
                y: -MathF.Sin(p),
                z: MathF.Cos(p) * MathF.Cos(y));
        }

        public readonly Look ToLook()
        {
            return new Look(RadYaw, RadPitch);
        }

        public readonly Vector2 ToVector2()
        {
            return UnsafeR.As<DegreeLook, Vector2>(this);
        }

        private readonly string GetDebuggerDisplay()
        {
            return ToString();
        }

        public readonly bool Equals(DegreeLook other)
        {
            return this == other;
        }

        public readonly bool Equals(Look other)
        {
            return this == other;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is DegreeLook dlook && Equals(dlook)
                || obj is Look look && Equals(look);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Yaw, Pitch);
        }

        public override readonly string ToString()
        {
            return "Y:" + Yaw + "°  P:" + Pitch + "°";
        }

        public static bool operator ==(DegreeLook a, DegreeLook b)
        {
            return a.Yaw == b.Yaw
                && a.Pitch == b.Pitch;
        }

        public static bool operator ==(DegreeLook a, Look b)
        {
            return a.RadYaw == b.Yaw
                && a.RadPitch == b.Pitch;
        }

        public static bool operator !=(DegreeLook a, DegreeLook b)
        {
            return !(a == b);
        }

        public static bool operator !=(DegreeLook a, Look b)
        {
            return !(a == b);
        }
    }
}
