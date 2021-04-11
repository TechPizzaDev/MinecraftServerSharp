using System;

namespace MCServerSharp.Blocks
{
    public class BooleanStateProperty : StateProperty<bool>
    {
        private static ReadOnlySpan<bool> Values => new bool[2] { false, true };

        public override int Count => 2;

        public BooleanStateProperty(string name) : base(name)
        {
        }

        public override int GetIndex(ReadOnlyMemory<char> value)
        {
            return GetIndex(bool.Parse(value.Span));
        }

        public override int GetIndex(bool value)
        {
            return value ? 1 : 0;
        }

        public override bool GetValue(int index)
        {
            return Values[index];
        }
    }
}
