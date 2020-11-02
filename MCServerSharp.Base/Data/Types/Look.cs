using System;
using System.Diagnostics;
using System.Numerics;
using MCServerSharp.Utility;

namespace MCServerSharp
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct Look : IEquatable<Look>, IEquatable<DegreeLook>
    {
        public float Yaw;
        public float Pitch;

        public readonly float DegYaw => Yaw / MathF.PI * 180;
        public readonly float DegPitch => Pitch / MathF.PI * 180;

        public Look(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public static Look FromVectors(Vector3 origin, Vector3 towards)
        {
            Vector3 d = towards - origin;
            float r = d.Length();
            
            float yaw = -MathF.Atan2(d.X, d.Z);
            if (yaw < 0)
                yaw = MathF.PI * 2 + yaw;

            float pitch = -MathF.Asin(d.Y / r);

            return new Look(yaw, pitch);
        }

        public readonly Vector3 ToUnitVector3()
        {
            return new Vector3(
                x: -MathF.Cos(Pitch) * MathF.Sin(Yaw),
                y: -MathF.Sin(Pitch),
                z: MathF.Cos(Pitch) * MathF.Cos(Yaw));
        }

        public readonly DegreeLook ToDegreeLook()
        {
            return new DegreeLook(DegYaw, DegPitch);
        }

        public readonly Vector2 ToVector2()
        {
            return UnsafeR.As<Look, Vector2>(this);
        }

        private readonly string GetDebuggerDisplay()
        {
            return ToString();
        }

        public readonly bool Equals(Look other)
        {
            return this == other;
        }

        public readonly bool Equals(DegreeLook other)
        {
            return this == other;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Look look && Equals(look)
                || obj is DegreeLook dlook && Equals(dlook);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Yaw, Pitch);
        }

        public override readonly string ToString()
        {
            return "Y:" + Yaw + "  P:"+ Pitch;
        }

        public static bool operator ==(Look a, Look b)
        {
            return a.Yaw == b.Yaw
                && a.Pitch == b.Pitch;
        }

        public static bool operator ==(Look a, DegreeLook b)
        {
            return a.Yaw == b.RadYaw
                && a.Pitch == b.RadPitch;
        }

        public static bool operator !=(Look a, Look b)
        {
            return !(a == b);
        }

        public static bool operator !=(Look a, DegreeLook b)
        {
            return !(a == b);
        }
    }
}
