using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Specifies the packet constructor.
    /// The properties are linked by parameter names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class PacketConstructorAttribute : Attribute
    {
    }
}