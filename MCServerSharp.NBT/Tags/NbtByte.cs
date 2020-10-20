using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtByte : NbTag
    {
        public override NbtType Type => NbtType.Byte;

        public byte Value { get; }

        public NbtByte(Utf8String? name, byte value) : base(name)
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
