using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtDouble : NbTag
    {
        public static NbtDouble Zero { get; } = new(0);
        public static NbtDouble One { get; } = new(1);
        public static NbtDouble MinValue { get; } = new(double.MinValue);
        public static NbtDouble MaxValue { get; } = new(double.MaxValue);
        public static NbtDouble Epsilon { get; } = new(double.Epsilon);
        public static NbtDouble NaN { get; } = new(double.NaN);
        public static NbtDouble NegativeInfinity { get; } = new(double.NegativeInfinity);
        public static NbtDouble PositiveInfinity { get; } = new(double.PositiveInfinity);

        protected double _value;

        public double Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.Double;

        public NbtDouble()
        {
        }

        public NbtDouble(double value)
        {
            Value = value;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Value);
        }
    }

    public class NbtMutDouble : NbtDouble
    {
        public new double Value { get => _value; set => _value = value; }

        public NbtMutDouble()
        {
        }

        public NbtMutDouble(double value)
        {
            Value = value;
        }
    }
}
