using System;
using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.NBT
{
    public abstract class NbtArray : NbTag
    {
        public int Length { get; }

        public NbtArray(int length, Utf8String? name = null) : base(name)
        {
            if (length < 0)
                throw new ArgumentNullException(nameof(length));

            Length = length;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Length);
        }
    }
}
