using System;

namespace MinecraftServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class PacketPropertyAttribute : Attribute
    {
        public int SerializationOrder { get; }

        public PacketPropertyAttribute(int serializationOrder)
        {
            SerializationOrder = serializationOrder;
        }
    }
}
