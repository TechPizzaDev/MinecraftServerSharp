using System;

namespace MinecraftServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class LengthConstraintAttribute : Attribute
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Constant { get; set; }

        public LengthConstraintAttribute()
        {
        }
    }
}

