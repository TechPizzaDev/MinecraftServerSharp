using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtShort : NbTag
    {
        public override NbtType Type => NbtType.Short;

        public short Value { get; }

        public NbtShort(Utf8String? name, short value) : base(name)
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
