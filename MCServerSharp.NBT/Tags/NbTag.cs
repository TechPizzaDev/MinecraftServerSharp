using System;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbTag
    {
        public abstract NbtType Type { get; }

        public virtual void WriteHeader(NetBinaryWriter writer, NbtFlags flags)
        {
            if (flags.HasFlag(NbtFlags.Typed))
            {
                writer.Write((byte)Type);
            }
        }

        public abstract void WritePayload(NetBinaryWriter writer, NbtFlags flags);

        public static NbtType GetNbtType<TTag>()
            where TTag : NbTag
        {
            if (typeof(TTag) == typeof(NbtByte))
                return NbtType.Byte;
            if (typeof(TTag) == typeof(NbtShort))
                return NbtType.Short;
            if (typeof(TTag) == typeof(NbtInt))
                return NbtType.Int;
            if (typeof(TTag) == typeof(NbtLong))
                return NbtType.Long;

            if (typeof(TTag) == typeof(NbtFloat))
                return NbtType.Float;
            if (typeof(TTag) == typeof(NbtDouble))
                return NbtType.Double;

            if (typeof(TTag) == typeof(NbtString))
                return NbtType.String;
            if (typeof(TTag) == typeof(NbtByteArray))
                return NbtType.ByteArray;
            if (typeof(TTag) == typeof(NbtIntArray))
                return NbtType.IntArray;
            if (typeof(TTag) == typeof(NbtLongArray))
                return NbtType.LongArray;

            if (typeof(TTag) == typeof(NbtCompound))
                return NbtType.Compound;
            if (typeof(TTag).GetGenericTypeDefinition() == typeof(NbtList<>))
                return NbtType.List;

            throw new ArgumentException("No matching NBT type for " + typeof(TTag) + ".");
        }
    }
}
