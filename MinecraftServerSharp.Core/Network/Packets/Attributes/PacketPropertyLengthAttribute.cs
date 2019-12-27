using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Used to dictate the length of the marked property by looking at another property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PacketPropertyLengthAttribute : Attribute
    {
        public string LengthPropertyName { get; }

        public PacketPropertyLengthAttribute(string lengthPropertyName)
        {
            LengthPropertyName = lengthPropertyName ?? 
                throw new ArgumentNullException(nameof(lengthPropertyName));
        }
    }
}