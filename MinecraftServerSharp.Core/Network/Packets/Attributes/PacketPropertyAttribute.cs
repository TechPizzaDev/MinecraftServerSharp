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
        /// Tells the serializer to read as the specified type 
        /// and then cast it back to the property.
        /// <para>
        /// Commonly used for enums that use variable integers.
        /// </para>
        /// </summary>
        public Type UnderlyingType { get; set; }
        
        /// <summary>
        /// Used to limit the length of arrays and strings.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Used to limit the size in bytes of byte arrays and strings.
        /// </summary>
        public int MaxByteLength { get; set; }

        public NetTextEncoding TextEncoding { get; set; } = NetTextEncoding.Utf8;

        public PacketPropertyAttribute(int serializationOrder)
        {
            SerializationOrder = serializationOrder;
        }
    }
}
