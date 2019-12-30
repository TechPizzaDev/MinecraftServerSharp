using System;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder
    {
        public readonly struct LengthFromAttributeInfo
        {
            public ParameterInfo Source { get; }
            public ParameterInfo Target { get; }

            public bool HasValue => Source != null;

            public LengthFromAttributeInfo(
                ParameterInfo sourceProperty,
                ParameterInfo targetProperty)
            {
                Source = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
                Target = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
            }
        }
    }
}
