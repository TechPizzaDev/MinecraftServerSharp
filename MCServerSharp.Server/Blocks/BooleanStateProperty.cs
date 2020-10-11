using System;

namespace MCServerSharp.Blocks
{
    public class BooleanStateProperty : StateProperty<bool>
    {
        public override int ValueCount => 2;

        public BooleanStateProperty(string name) : base(name)
        {
        }

        public override int ParseIndex(string value)
        {
            return GetIndex(bool.Parse(value));
        }

        public override int GetIndex(bool value)
        {
            return value ? 1 : 0;
        }

        public override bool GetValue(int index)
        {
            return index switch
            {
                0 => false,
                1 => true,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
    }
}
