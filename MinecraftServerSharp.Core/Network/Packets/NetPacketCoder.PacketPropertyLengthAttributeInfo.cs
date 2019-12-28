using System;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder
    {
        public class PacketPropertyLengthAttributeInfo
        {
            public PacketPropertyInfo SourceProperty { get; }
            public PacketPropertyInfo TargetProperty { get; }

            public PacketPropertyLengthAttributeInfo(
                PacketPropertyInfo sourceProperty, PacketPropertyInfo targetProperty)
            {
                SourceProperty = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
                TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
            }
        }
    }
}
