using System;

namespace MinecraftServerSharp.NBT
{
    public static class NbTagExtensions
    {
        public static NbtCompound AsCompound(this NbTag tag, Utf8String? name)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (tag.Name == null)
                throw new InvalidOperationException("This tag is nameless.");

            return new NbtCompound(name).Add(tag);
        }
    }
}
