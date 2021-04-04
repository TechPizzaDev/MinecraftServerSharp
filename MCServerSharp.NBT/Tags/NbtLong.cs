using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLong : NbTag
    {
        public static NbtLong Zero { get; } = new(0);
        public static NbtLong One { get; } = new(1);
        public static NbtLong MinValue { get; } = new(long.MinValue);
        public static NbtLong MaxValue { get; } = new(long.MaxValue);

        protected long _value;

        public long Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Long;

        public NbtLong()
        {
        }

        public NbtLong(long value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }

    public class NbtMutLong : NbtLong
    {
        public new long Value { get => _value; set => _value = value; }

        public NbtMutLong()
        {
        }

        public NbtMutLong(long value)
        {
            Value = value;
        }
    }
}
