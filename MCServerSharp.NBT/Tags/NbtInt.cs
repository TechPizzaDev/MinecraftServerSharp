using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtInt : NbTag
    {
        public static NbtInt Zero { get; } = new(0);
        public static NbtInt One { get; } = new(1);
        public static NbtInt MinValue { get; } = new(int.MinValue);
        public static NbtInt MaxValue { get; } = new(int.MaxValue);

        protected int _value;

        public int Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Int;

        public NbtInt()
        {
        }

        public NbtInt(int value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }
    
    public class NbtMutInt : NbtInt
    {
        public new int Value { get => _value; set => _value = value; }

        public NbtMutInt()
        {
        }

        public NbtMutInt(int value)
        {
            Value = value;
        }
    }
}
