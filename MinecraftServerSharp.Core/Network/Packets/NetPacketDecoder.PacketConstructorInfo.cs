using System;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public partial class NetPacketDecoder
    {
        public readonly struct PacketConstructorInfo
        {
            public ConstructorInfo Constructor { get; }
            public PacketConstructorAttribute Attribute { get; }

            public PacketConstructorInfo(ConstructorInfo constructor, PacketConstructorAttribute attribute)
            {
                Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
                Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }
        }
    }
}
