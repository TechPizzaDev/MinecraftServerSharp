using System;

namespace MCServerSharp
{
    public partial class Utf8String
    {
        public static SpanRangeSplitter<byte> EnumerateRangeSplit(
            ReadOnlySpan<byte> value,
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return new SpanRangeSplitter<byte>(value, separator, splitOptions, maxCount);
        }

        public SpanRangeSplitter<byte> EnumerateRangeSplit(
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return EnumerateRangeSplit(Bytes.Span, separator, splitOptions, maxCount);
        }

        public SpanRangeSplitter<byte> EnumerateRangeSplit(
            Utf8String? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return EnumerateRangeSplit(separator.AsSpan(), splitOptions, maxCount);
        }
    }
}
