using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtInt : NbTag
    {
        public override NbtType Type => NbtType.Int;

        public int Value { get; set; }

        public NbtInt()
        {
        }

        public NbtInt(int value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
