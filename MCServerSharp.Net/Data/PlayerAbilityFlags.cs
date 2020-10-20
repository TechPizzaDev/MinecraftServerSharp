using System;

namespace MCServerSharp.Net.Packets
{
    [Flags]
    public enum PlayerAbilityFlags : byte
    {
        Invulnerable = 0x01,
        Flying = 0x02,
        AllowFlying = 0x04,
        CreativeMode = 0x08
    }
}
