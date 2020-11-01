using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtDouble : NbTag
    {
        public override NbtType Type => NbtType.Double;

        public double Value { get; set; }

        public NbtDouble()
        {
        }

        public NbtDouble(double value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
