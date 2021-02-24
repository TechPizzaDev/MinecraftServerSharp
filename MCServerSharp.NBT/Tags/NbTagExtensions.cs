using System;

namespace MCServerSharp.NBT
{
    public static class NbTagExtensions
    {
        public static NbtCompound ToCompound(this NbTag tag, Utf8String? compoundName, Utf8String tagName)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            return new NbtCompound(compoundName).Add(tagName, tag);
        }
    }
}
