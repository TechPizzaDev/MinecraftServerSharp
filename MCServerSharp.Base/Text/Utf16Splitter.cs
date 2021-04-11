using System;
using System.Buffers;
using System.Text;

namespace MCServerSharp
{
    public ref struct Utf16Splitter
    {
        private ReadOnlySpanSplitter<char> _splitter;
        private int _offset;

        public ReadOnlySpan<char> Span => _splitter.Span;
        public ReadOnlySpan<char> Separator => _splitter.Separator;
        public StringSplitOptions SplitOptions => _splitter.SplitOptions;

        public Range Current { get; private set; }

        public Utf16Splitter(ReadOnlySpan<char> span, ReadOnlySpan<char> separator, StringSplitOptions splitOptions)
        {
            ReadOnlySpan<char> separatorSlice = separator;
            do
            {
                var separatorStatus = Rune.DecodeFromUtf16(separatorSlice, out _, out int consumed);
                if (separatorStatus == OperationStatus.InvalidData)
                    throw new ArgumentException("The separator is not valid UTF16.", nameof(separator));
                separatorSlice = separatorSlice[consumed..];
            }
            while (separatorSlice.Length > 0);

            _splitter = new ReadOnlySpanSplitter<char>(span, separator, splitOptions);
            _offset = 0;

            Current = default;
        }

        public Utf16Splitter GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_splitter.Separator.IsEmpty)
            {
                var status = Rune.DecodeFromUtf16(Span.Slice(_offset), out _, out int consumed);
                Current = new Range(_offset, _offset += consumed);
                return status != OperationStatus.NeedMoreData;
            }

            bool move = _splitter.MoveNext();
            Current = _splitter.Current;
            return move;
        }
    }
}
