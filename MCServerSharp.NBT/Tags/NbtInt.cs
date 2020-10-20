using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtInt : NbTag
    {
        public override NbtType Type => NbtType.Int;

        public int Value { get; }

        public NbtInt(Utf8String? name, int value) : base(name)
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
