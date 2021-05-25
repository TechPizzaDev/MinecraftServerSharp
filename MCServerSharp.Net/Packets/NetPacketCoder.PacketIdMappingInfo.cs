﻿using System;
using System.Reflection;

namespace MCServerSharp.Net.Packets
{
    public abstract partial class NetPacketCoder<TPacketId>
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
