using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtByteArray : NbtArray<sbyte>
    {
        public override NbtType Type => NbtType.ByteArray;

        public NbtByteArray(sbyte[] items) : base(items)
        {
        }

        public NbtByteArray(int count) : base(count)
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            base.WritePayload(writer, flags);

            writer.Write(Items);
        }
    }
}
