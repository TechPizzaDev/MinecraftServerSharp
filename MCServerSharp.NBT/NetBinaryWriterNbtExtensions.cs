using System;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public static class NetBinaryWriterNbtExtensions
    {
        public static void Write(this NetBinaryWriter writer, NbTag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            tag.Write(writer, NbtFlags.TypedNamed);
        }

        public static void Write(this NetBinaryWriter writer, NbtCompound compound)
        {
            if (compound == null)
                throw new ArgumentNullException(nameof(compound));

            compound.Write(writer, NbtFlags.TypedNamed);
        }
    }
}
