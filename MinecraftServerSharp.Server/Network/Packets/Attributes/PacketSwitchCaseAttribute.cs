using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Marks a parameter to be treated as a switch case for union packets.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class PacketSwitchCaseAttribute : Attribute
    {
        public object MatchCase { get; }

        public PacketSwitchCaseAttribute(object matchCase)
        {
            MatchCase = matchCase ?? throw new ArgumentNullException(nameof(matchCase));
        }
    }
}
