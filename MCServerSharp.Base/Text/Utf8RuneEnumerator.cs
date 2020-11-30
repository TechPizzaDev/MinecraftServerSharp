using System;
using System.Text;

namespace MCServerSharp.Text
{
    public ref struct Utf8RuneEnumerator
    {
        private ReadOnlySpan<byte> _utf8;
        private Rune _current;

        public Rune Current => _current;

        public Utf8RuneEnumerator(ReadOnlySpan<byte> utf8)
        {
            _utf8 = utf8;
            _current = default;
        }

        public bool MoveNext()
        {
            var status = Rune.DecodeFromUtf8(_utf8, out _current, out int consumed);
            _utf8 = _utf8[consumed..]; 
            return status != System.Buffers.OperationStatus.NeedMoreData;
        }

        public Utf8RuneEnumerator GetEnumerator()
        {
            return this;
        }
    }
}
