using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtIntArray : NbtArray<int>
    {
        public override NbtType Type => NbtType.IntArray;

        public NbtIntArray(Utf8String? name, int count) : base(name, count)
        {
        }

        public NbtIntArray(int count) : base(null, count)
        {
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Items);
        }
    }
}
