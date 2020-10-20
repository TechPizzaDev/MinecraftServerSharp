using System;
using System.Diagnostics;
using System.Reflection;

namespace MCServerSharp.Net.Packets
{
    public partial class NetPacketEncoder
    {
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public class DataPropertyInfo
        {
            public PropertyInfo Property { get; }
            public DataPropertyAttribute PropertyAttrib { get; }
            public DataLengthConstraintAttribute? LengthConstraintAttrib { get; }

            public Type Type => Property.PropertyType;
            public string Name => Property.Name;

            public int Order => PropertyAttrib.Order;

            public DataPropertyInfo(
                PropertyInfo property,
                DataPropertyAttribute propertyAttrib,
                DataLengthConstraintAttribute? lengthConstraintAttrib)
            {
                Property = property ?? throw new ArgumentNullException(nameof(property));
                PropertyAttrib = propertyAttrib ?? throw new ArgumentNullException(nameof(propertyAttrib));
                LengthConstraintAttrib = lengthConstraintAttrib;
            }

            private string GetDebuggerDisplay()
            {
                return "@" + Order + " " + Property.ToString();
            }
        }
    }
}
