using System;

namespace MinecraftServerSharp.Network.Packets
{
    public partial class NetPacketCoder
    {
        public readonly struct PropertyLengthAttributeInfo
        {
            public PacketPropertyInfo SourceProperty { get; }
            public PacketPropertyInfo TargetProperty { get; }

            public PropertyLengthAttributeInfo(
                PacketPropertyInfo sourceProperty, PacketPropertyInfo targetProperty)
            {
                SourceProperty = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
                TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
            }
        }
    }
}
