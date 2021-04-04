using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtShort : NbTag
    {
        public static NbtShort Zero { get; } = new(0);
        public static NbtShort One { get; } = new(1);
        public static NbtShort MinValue { get; } = new(short.MinValue);
        public static NbtShort MaxValue { get; } = new(short.MaxValue);

        protected short _value;

        public short Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Short;

        public NbtShort()
        {
        }

        public NbtShort(short value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }

    public class NbtMutShort : NbtShort
    {
        public new short Value { get => _value; set => _value = value; }

        public NbtMutShort()
        {
        }

        public NbtMutShort(short value)
        {
            Value = value;
        }
    }
}
