using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MCServerSharp.Text
{
    public ref struct Utf16Enumerator
    {
        private IEnumerator<char>? _charEnumerator;
        private RuneEnumerator _runeEnumerator;
        private int _length;
        private int _offset;
        private int _store;

        public char Current { get; private set; }

        public Utf16Enumerator(RuneEnumerator runes) : this()
        {
            _runeEnumerator = runes;
        }

        public Utf16Enumerator(IEnumerator<Rune>? runes) : this(new RuneEnumerator(runes))
        {
        }

        public Utf16Enumerator(IEnumerator<char>? chars) : this()
        {
            _charEnumerator = chars;
        }

        public bool MoveNext()
        {
            if (_charEnumerator != null)
            {
                bool move = _charEnumerator.MoveNext();
                Current = _charEnumerator.Current;
                return move;
            }

            Span<int> store = MemoryMarshal.CreateSpan(ref _store, 1);
            Span<char> chars = MemoryMarshal.Cast<int, char>(store);

            if (_offset == _length)
            {
                if (!_runeEnumerator.MoveNext())
                    return false;

                _length = _runeEnumerator.Current.EncodeToUtf16(chars);
                _offset = 0;
            }

            Current = chars[_offset++];
            return true;
        }

        public Utf16Enumerator GetEnumerator()
        {
            return this;
        }

        public static implicit operator Utf16Enumerator(RuneEnumerator text)
        {
            return new Utf16Enumerator(text);
        }
    }
}
