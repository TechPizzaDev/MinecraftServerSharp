using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MCServerSharp.Text
{
    public ref struct Utf8Enumerator
    {
        private IEnumerator<byte>? _byteEnumerator;
        private RuneEnumerator _runeEnumerator;
        private int _length;
        private int _offset;
        private int _store;
        
        public byte Current { get; private set; }

        public Utf8Enumerator(RuneEnumerator runes) : this()
        {
            _runeEnumerator = runes;
        }

        public Utf8Enumerator(IEnumerator<Rune>? runes) : this(new RuneEnumerator(runes))
        {
        }

        public Utf8Enumerator(IEnumerator<byte>? chars) : this()
        {
            _byteEnumerator = chars;
        }

        public bool MoveNext()
        {
            if (_byteEnumerator != null)
            {
                bool move = _byteEnumerator.MoveNext();
                Current = _byteEnumerator.Current;
                return move;
            }

            Span<int> store = MemoryMarshal.CreateSpan(ref _store, 1);
            Span<byte> chars = MemoryMarshal.Cast<int, byte>(store);

            if (_offset == _length)
            {
                if (!_runeEnumerator.MoveNext())
                    return false;

                _length = _runeEnumerator.Current.EncodeToUtf8(chars);
                _offset = 0;
            }

            Current = chars[_offset++];
            return true;
        }

        public Utf8Enumerator GetEnumerator()
        {
            return this;
        }

        public static implicit operator Utf8Enumerator(RuneEnumerator text)
        {
            return new Utf8Enumerator(text);
        }
    }
}
