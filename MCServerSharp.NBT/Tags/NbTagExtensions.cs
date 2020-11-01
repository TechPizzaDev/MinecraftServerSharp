using System;

namespace MCServerSharp.NBT
{
    public static class NbTagExtensions
    {
        public static NbtCompound AsCompound(this NbTag tag, Utf8String tagName, Utf8String? compoundName = null)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            return new NbtCompound(compoundName).Add(tagName, tag);
        }
    }
}
