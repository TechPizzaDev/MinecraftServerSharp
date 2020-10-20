using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtFloat : NbTag
    {
        public override NbtType Type => NbtType.Float;

        public float Value { get; }

        public NbtFloat(Utf8String? name, float value) : base(name)
        {
            Value = value;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Value);
        }
    }
}
