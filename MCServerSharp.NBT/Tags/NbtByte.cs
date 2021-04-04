using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtByte : NbTag
    {
        public static NbtByte Zero { get; } = new(0);
        public static NbtByte One { get; } = new(1);
        public static NbtByte MinValue { get; } = new(sbyte.MinValue);
        public static NbtByte MaxValue { get; } = new(sbyte.MaxValue);

        protected sbyte _value;

        public sbyte Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Byte;

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

    public class NbtMutByte : NbtByte
    {
        public new sbyte Value { get => _value; set => _value = value; }

        public NbtMutByte()
        {
        }

        public NbtMutByte(sbyte value)
        {
            Value = value;
        }
    }
}
