using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLongArray : NbtArray<long>
    {
        public override NbtType Type => NbtType.LongArray;

        public NbtLongArray(long[] items) : base(items)
        {
        }

        public NbtLongArray(int count) : base(count)
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            base.WritePayload(writer, flags);

            writer.Write(Items);
        }
    }
}
