using System;
using System.Buffers.Text;

namespace MCServerSharp.Blocks
{
    public class BooleanStateProperty : StateProperty<bool>
    {
        public override int Count => 2;

        public BooleanStateProperty(string name) : base(name)
        {
        }

        public override int GetIndex(ReadOnlyMemory<char> value)
        {
            return GetIndex(bool.Parse(value.Span));
        }

        public override int GetIndex(Utf8Memory value)
        {
            _ = Utf8Parser.TryParse(value.Span, out bool rawValue, out _);
            return GetIndex(rawValue);
        }

        public override int GetIndex(bool value)
        {
            return value ? 1 : 0;
        }

        public override bool GetValue(int index)
        {
            return index == 1;
        }
    }
}
