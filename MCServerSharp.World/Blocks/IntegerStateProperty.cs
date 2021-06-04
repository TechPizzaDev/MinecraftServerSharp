using System;
using System.Buffers.Text;
using System.Globalization;

namespace MCServerSharp.Blocks
{
    public class IntegerStateProperty : StateProperty<int>
    {
        public int Min { get; }
        public int Max { get; }

        public override int Count => Max - Min;

        public IntegerStateProperty(string name, int min, int max) : base(name)
        {
            if (min > max)
                throw new ArgumentException("Minimum is greater than maximum.", nameof(max));
            Min = min;
            Max = max;
        }

        public override int GetIndex(ReadOnlyMemory<char> value)
        {
            return GetIndex(int.Parse(value.Span, NumberStyles.Integer, CultureInfo.InvariantCulture));
        }

        public override int GetIndex(Utf8Memory value)
        {
            bool success = Utf8Parser.TryParse(value.Span, out int rawValue, out _);
            return GetIndex(rawValue);
        }

        public override int GetIndex(int value)
        {
            if (value > Max)
                throw new ArgumentOutOfRangeException(nameof(value));

            int index = value - Min;
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            return index;
        }

        public override int GetValue(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            int value = Min + index;
            if (value > Max)
                throw new ArgumentOutOfRangeException(nameof(index));

            return value;
        }
    }
}
