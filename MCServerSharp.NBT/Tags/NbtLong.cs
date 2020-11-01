using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLong : NbTag
    {
        public override NbtType Type => NbtType.Long;

        public long Value { get; set; }

        public NbtLong()
        {
        }

        public NbtLong(long value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
