using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DataPropertyAttribute : Attribute
    {
        public int Order { get; }

        public DataPropertyAttribute(int order)
        {
            Order = order;
        }
    }
}
