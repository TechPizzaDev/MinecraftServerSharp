using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtFloat : NbTag
    {
        public static NbtFloat Zero { get; } = new(0);
        public static NbtFloat One { get; } = new(1);
        public static NbtFloat MinValue { get; } = new(float.MinValue);
        public static NbtFloat MaxValue { get; } = new(float.MaxValue);
        public static NbtFloat Epsilon { get; } = new(float.Epsilon);
        public static NbtFloat NaN { get; } = new(float.NaN);
        public static NbtFloat NegativeInfinity { get; } = new(float.NegativeInfinity);
        public static NbtFloat PositiveInfinity { get; } = new(float.PositiveInfinity);

        protected float _value;

        public float Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Float;

        public NbtFloat()
        {
        }

        public NbtFloat(float value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }

    public class NbtMutFloat : NbtFloat
    {
        public new float Value { get => _value; set => _value = value; }

        public NbtMutFloat()
        {
        }

        public NbtMutFloat(float value)
        {
            Value = value;
        }
    }
}
