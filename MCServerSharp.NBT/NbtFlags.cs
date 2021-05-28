using System;

namespace MCServerSharp.NBT
{
    [Flags]
    public enum NbtFlags : byte
    {
        None = 0,
        Typed = 1 << 0,
        Named = 1 << 1,
        
        TypedNamed = Typed | Named
    }

}
