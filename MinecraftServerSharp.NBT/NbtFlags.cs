using System;

namespace MinecraftServerSharp.NBT
{
    [Flags]
    public enum NbtFlags
    {
        None = 0,
        Named = 1 << 0,
        Typed = 1 << 1
    }

}
