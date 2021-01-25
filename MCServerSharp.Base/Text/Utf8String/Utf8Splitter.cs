using System;
using System.Buffers;
using System.Text;

namespace MCServerSharp
{
    public ref struct Utf8Splitter
    {
        private ReadOnlySpanSplitter<byte> _splitter;
        private int _offset;

        public ReadOnlySpan<byte> Span => _splitter.Span;
        public ReadOnlySpan<byte> Separator => _splitter.Separator;
        public StringSplitOptions SplitOptions => _splitter.SplitOptions;

        public Range Current { get; private set; }

        public Utf8Splitter(ReadOnlySpan<byte> span, ReadOnlySpan<byte> separator, StringSplitOptions splitOptions)
        {
            ReadOnlySpan<byte> separatorSlice = separator;
            do
            {
                var separatorStatus = Rune.DecodeFromUtf8(separatorSlice, out _, out int consumed);
                if (separatorStatus == OperationStatus.InvalidData)
                    throw new ArgumentException("The separator is not valid UTF8.", nameof(separator));
                separatorSlice = separatorSlice[consumed..];
            }
            while (separatorSlice.Length > 0);

            _splitter = new ReadOnlySpanSplitter<byte>(span, separator, splitOptions);
            _offset = 0;

            Current = default;
        }

        public Utf8Splitter GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_splitter.Separator.IsEmpty)
            {
                var status = Rune.DecodeFromUtf8(Span.Slice(_offset), out _, out int consumed);
                Current = new Range(_offset, _offset += consumed);
                return status != OperationStatus.NeedMoreData;
            }

            Current = _splitter.Current;
            return _splitter.MoveNext();
        }
    }
}
