using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Specifies the packet constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class PacketConstructorAttribute : Attribute
    {
    }
}