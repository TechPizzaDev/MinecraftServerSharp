using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtFloat : NbTag
    {
        public override NbtType Type => NbtType.Float;

        public float Value { get; set; }

        public NbtFloat()
        {
        }

        public NbtFloat(float value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
