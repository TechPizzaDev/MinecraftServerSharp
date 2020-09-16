using System;
using System.Reflection;

namespace MCServerSharp.Net.Packets
{
    public partial class NetPacketEncoder
    {
        public class PacketPropertyInfo
        {
            public PropertyInfo Property { get; }
            public PacketPropertyAttribute Attribute { get; }
            public LengthConstraintAttribute? LengthConstraint { get; }

            public Type Type => Property.PropertyType;
            public string Name => Property.Name;
            public int SerializationOrder => Attribute.SerializationOrder;

            public PacketPropertyInfo(
                PropertyInfo property,
                PacketPropertyAttribute attribute,
                LengthConstraintAttribute? lengthConstraint)
            {
                Property = property ?? throw new ArgumentNullException(nameof(property));
                Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
                LengthConstraint = lengthConstraint;
            }
        }
    }
}
