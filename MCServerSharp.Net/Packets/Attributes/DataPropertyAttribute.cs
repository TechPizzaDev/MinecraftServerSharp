using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DataPropertyAttribute : Attribute
    {
        public int Order { get; }
        public DataSerializeMode SerializeMode { get; }

        public DataPropertyAttribute(int order, DataSerializeMode serializeMode = DataSerializeMode.Auto)
        {
            Order = order;
            SerializeMode = serializeMode;
        }
    }
}
