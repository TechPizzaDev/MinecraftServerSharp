using System;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCodec<TPacketID>
        where TPacketID : Enum
    {
        private class PacketIDMappingInfo
        {
            public FieldInfo Field { get; }
            public PacketIDMappingAttribute Attribute { get; }

            public PacketIDMappingInfo(FieldInfo field, PacketIDMappingAttribute attribute)
            {
                Field = field ?? throw new ArgumentNullException(nameof(field));
                Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }
        }
    }
}
