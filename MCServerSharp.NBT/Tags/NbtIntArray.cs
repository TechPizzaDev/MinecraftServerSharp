using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtIntArray : NbtArray<int>
    {
        public override NbtType Type => NbtType.IntArray;

        public NbtIntArray(int[] items) : base(items)
        {
        }

        public NbtIntArray(int count) : base(count)
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            base.WritePayload(writer, flags);

            writer.Write(Items);
        }
    }
}
