using System;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    /*
    public abstract partial class NetPacketCoder
    {
        /// <summary>
        /// may be useful in NetPacketEncoder
        /// </summary>
        public class PacketPropertyInfo
        {
            public PropertyInfo PropertyInfo { get; }
            //public PacketPropertyAttribute PropertyAttribute { get; }
            public LengthFromAttribute LengthAttribute { get; }

            public string Name => PropertyInfo.Name;
            public int SerializationOrder => PropertyAttribute.SerializationOrder;

            public Type Type => PropertyInfo.PropertyType;
            
            public PacketPropertyInfo(
                PropertyInfo propertyInfo,
                //PacketPropertyAttribute propertyAttribute,
                LengthFromAttribute lengthAttribute = null)
            {
                PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
                PropertyAttribute = propertyAttribute ?? throw new ArgumentNullException(nameof(propertyAttribute));
                LengthAttribute = lengthAttribute;
            }
        }
    }
    */
}
