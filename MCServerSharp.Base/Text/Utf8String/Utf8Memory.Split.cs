using System;

namespace MCServerSharp
{
    public readonly partial struct Utf8Memory
    {
        public Utf8Splitter EnumerateSplit(
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return new Utf8Splitter(Span, separator, splitOptions);
        }

        public Utf8Splitter EnumerateSplit(
            Utf8Memory separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(separator.Span, splitOptions);
        }

        public Utf8Splitter EnumerateSplit(
            Utf8String? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(separator.AsSpan(), splitOptions);
        }
    }
}
