using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Used for marking properties that should be considered when serializing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PacketPropertyAttribute : Attribute
    {
        public int SerializationOrder { get; }

        /// <summary>
        /// Used to limit the length of arrays and strings.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Used to limit the size in bytes of byte arrays and strings.
        /// </summary>
        public int MaxByteLength { get; set; }

        public PacketPropertyAttribute(int serializationOrder)
        {
            SerializationOrder = serializationOrder;
        }
    }
}
