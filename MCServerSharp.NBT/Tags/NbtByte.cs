using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtByte : NbTag
    {
        public override NbtType Type => NbtType.Byte;

        public sbyte Value { get; set; }

        public NbtByte()
        {
        }

        public NbtByte(sbyte value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
}
