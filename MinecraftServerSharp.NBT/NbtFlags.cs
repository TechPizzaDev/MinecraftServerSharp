using System;

namespace MinecraftServerSharp.NBT
{
    [Flags]
    public enum NbtFlags
    {
        None = 0,
        Typed = 1 << 0,
        Named = 1 << 1,
        LittleEndian = 1 << 2,
        BigEndian = 1 << 3,

        TypedNamed = Typed | Named
    }

}
