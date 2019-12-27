using System;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public partial class NetPacketCoder
    {
        public class PacketPropertyInfo
        {
            public PropertyInfo PropertyInfo { get; }
            public PacketPropertyAttribute PropertyAttribute { get; }
            public PacketPropertyLengthAttribute LengthAttribute { get; }

            public int SerializationOrder => PropertyAttribute.SerializationOrder;

            public PacketPropertyInfo(
                PropertyInfo propertyInfo,
                PacketPropertyAttribute propertyAttribute,
                PacketPropertyLengthAttribute lengthAttribute = null)
            {
                PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
                PropertyAttribute = propertyAttribute ?? throw new ArgumentNullException(nameof(propertyAttribute));
                LengthAttribute = lengthAttribute;
            }
        }
    }
}
