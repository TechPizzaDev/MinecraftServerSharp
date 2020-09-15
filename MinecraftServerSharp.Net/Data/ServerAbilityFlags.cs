using System;

namespace MinecraftServerSharp.Net.Packets
{
    [Flags]
    public enum ServerAbilityFlags : byte
    {
        Invulnerable = 0x01,
        Flying = 0x02,
        AllowFlying = 0x04,
        CreativeMode = 0x08
    }
}
