using System;
using System.Reflection;

namespace MinecraftServerSharp.Net.Packets
{
    public partial class NetPacketDecoder
    {
        public class PacketConstructorInfo
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
