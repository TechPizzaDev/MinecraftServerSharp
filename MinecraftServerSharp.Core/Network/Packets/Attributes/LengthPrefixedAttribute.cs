using System;

namespace MinecraftServerSharp.Network.Packets
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class LengthPrefixedAttribute : Attribute
    {
        public Type LengthType { get; }

        public LengthPrefixedAttribute(Type lengthType)
        {
            LengthType = lengthType ?? throw new ArgumentNullException(nameof(lengthType));
        }
    }
}
