using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtDouble : NbTag
    {
        public override NbtType Type => NbtType.Double;

        public double Value { get; }

        public NbtDouble(Utf8String? name, double value) : base(name)
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
