using System;

namespace MCServerSharp.NBT
{
    public static class NbTagExtensions
    {
        public static NbtMutCompound ToCompound(this NbTag tag, Utf8String? compoundName, Utf8String tagName)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            return new NbtMutCompound(compoundName).Add(tagName, tag);
        }
    }
}
