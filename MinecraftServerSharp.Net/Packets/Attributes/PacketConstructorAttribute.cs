using System;

namespace MCServerSharp.Net.Packets
{
    /// <summary>
    /// Specifies a packet constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class PacketConstructorAttribute : Attribute
    {
    }
}