using System;
using System.Reflection;

namespace MinecraftServerSharp.Net.Packets
{
    public abstract partial class NetPacketCoder<TPacketId>
        where TPacketId : Enum
    {
        private class PacketIdMappingInfo
        {
            public FieldInfo Field { get; }
            public PacketIdMappingAttribute Attribute { get; }

            public PacketIdMappingInfo(FieldInfo field, PacketIdMappingAttribute attribute)
            {
                Field = field ?? throw new ArgumentNullException(nameof(field));
                Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }
        }
    }
}
