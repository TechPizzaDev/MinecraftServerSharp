using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtShort : NbTag
    {
        public override NbtType Type => NbtType.Short;

        public short Value { get; set; }

        public NbtShort()
        {
        }

        public NbtShort(short value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
