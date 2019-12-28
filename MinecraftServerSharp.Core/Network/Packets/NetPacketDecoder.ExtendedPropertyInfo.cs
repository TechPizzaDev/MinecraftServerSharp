using System;
using System.Collections.Generic;

namespace MinecraftServerSharp.Network.Packets
{
    public partial class NetPacketDecoder
    {
        public readonly struct ExtendedPropertyInfo
        {
            public PacketPropertyInfo PropertyInfo { get; }
            public List<PacketPropertyInfo> ReadProperties { get; }
            public PacketPropertyLengthAttributeInfo LengthAttributeInfo { get; }

            public ExtendedPropertyInfo(
                PacketPropertyInfo propertyInfo, 
                List<PacketPropertyInfo> readProperties,
                PacketPropertyLengthAttributeInfo lengthAttributeInfo)
            {
                PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
                ReadProperties = readProperties ?? throw new ArgumentNullException(nameof(readProperties));
                LengthAttributeInfo = lengthAttributeInfo;
            }
        }
    }
}
