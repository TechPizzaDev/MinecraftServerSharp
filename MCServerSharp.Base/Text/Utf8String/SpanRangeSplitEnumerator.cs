using System;

namespace MCServerSharp
{
    public ref struct SpanRangeSplitEnumerator<T>
        where T : IEquatable<T>
    {
        private int _offset;
        private bool _isLastSeparator;

        public ReadOnlySpan<T> Value { get; }
        public ReadOnlySpan<T> Separator { get; }
        public StringSplitOptions SplitOptions { get; }
        public int? MaxCount { get; }

        public Range Current { get; private set; }

        public SpanRangeSplitEnumerator(
            ReadOnlySpan<T> value,
            ReadOnlySpan<T> separator,
            StringSplitOptions splitOptions,
            int? maxCount)
            : this()
        {
            if (separator.IsEmpty)
                splitOptions &= ~StringSplitOptions.TrimEntries;

            Value = value;
            Separator = separator;
            SplitOptions = splitOptions;
            MaxCount = maxCount;
        }

        public bool MoveNext()
        {
            if (_isLastSeparator)
            {
                _isLastSeparator = false;
                Current = Range.StartAt(Index.End);
                return true;
            }

            ReadOnlySpan<T> value = Value;
            ReadOnlySpan<T> separator = Separator;
            int start = _offset;

            for (; _offset < value.Length; _offset++)
            {
                if (separator.IsEmpty)
                {
                    Current = new Range(start, _offset++);
                    return true;
                }

                if (value[_offset].Equals(separator[0]) &&
                    separator.Length <= value.Length - _offset)
                {
                    if (separator.Length == 1 ||
                        value.Slice(_offset, separator.Length).SequenceEqual(separator))
                    {
                        Current = new Range(start, _offset);
                        _offset += separator.Length;

                        if (SplitOptions.HasAnyFlag(StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (_offset - start == 1)
                            {
                                start = _offset;
                                continue;
                            }
                        }
                        else
                        {
                            if (_offset == value.Length)
                                _isLastSeparator = true;
                        }
                        return true;
                    }
                }
            }

            if (start != _offset)
            {
                Current = new Range(start, _offset);
                (int _, int length) = Current.GetOffsetAndLength(value.Length);
                return length > 0;
            }

            Current = default;
            return false;
        }

        public SpanRangeSplitEnumerator<T> GetEnumerator()
        {
            return this;
        }
    }
}
