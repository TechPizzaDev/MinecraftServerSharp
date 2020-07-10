using System;

namespace MinecraftServerSharp.Network.Packets
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class PacketTypeSwitchAttribute : Attribute
    {
        public object MatchCase { get; }

        public PacketTypeSwitchAttribute(object matchCase)
        {
            MatchCase = matchCase ?? throw new ArgumentNullException(nameof(matchCase));
        }
    }
}
