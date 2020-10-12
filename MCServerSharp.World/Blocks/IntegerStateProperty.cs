using System;
using System.Globalization;

namespace MCServerSharp.Blocks
{
    public class IntegerStateProperty : StateProperty<int>
    {
        public int Min { get; }
        public int Max { get; }

        public override int ValueCount => Max - Min;

        public IntegerStateProperty(string name, int min, int max) : base(name)
        {
            if (min > max)
                throw new ArgumentException("Minimum is greater than maximum.", nameof(max));
            Min = min;
            Max = max;
        }

        public override int ParseIndex(string value)
        {
            return GetIndex(int.Parse(value, CultureInfo.InvariantCulture));
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
