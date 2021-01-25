using System;

namespace MCServerSharp
{
    public ref struct ReadOnlySpanSplitter<T>
        where T : IEquatable<T>
    {
        public ReadOnlySpan<T> Span { get; }
        public ReadOnlySpan<T> Separator { get; }
        public StringSplitOptions SplitOptions { get; }

        private readonly int _separatorLength;
        private readonly bool _initialized;

        private int _startCurrent;
        private int _endCurrent;
        private int _startNext;

        public Range Current => new Range(_startCurrent, _endCurrent);
        
        public ReadOnlySpanSplitter(
            ReadOnlySpan<T> span,
            ReadOnlySpan<T> separator, 
            StringSplitOptions splitOptions)
        {
            _initialized = true;
            Span = span;
            Separator = separator;
            _separatorLength = Separator.Length != 0 ? Separator.Length : 1;
            SplitOptions = splitOptions;

            _startCurrent = 0;
            _endCurrent = 0;
            _startNext = 0;
        }

        public ReadOnlySpanSplitter<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            TrySlice:
            if (!_initialized || _startNext > Span.Length)
            {
                return false;
            }
            _startCurrent = _startNext;

            ReadOnlySpan<T> slice = Span.Slice(_startCurrent);
            int separatorIndex = slice.IndexOf(Separator);
            int elementLength = separatorIndex != -1 ? separatorIndex : slice.Length;

            _endCurrent = _startCurrent + elementLength;
            _startNext = _endCurrent + _separatorLength;

            if ((SplitOptions & StringSplitOptions.RemoveEmptyEntries) != 0 &&
                _endCurrent - _startCurrent == 0)
            {
                goto TrySlice;
            }
            return true;
        }
    }
}
