using System;

namespace MCServerSharp
{
    public partial class Utf8String
    {
        public static SpanRangeSplitEnumerator<byte> EnumerateRangeSplit(
            ReadOnlySpan<byte> value,
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return new SpanRangeSplitEnumerator<byte>(value, separator, splitOptions, maxCount);
        }

        public SpanRangeSplitEnumerator<byte> EnumerateRangeSplit(
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return EnumerateRangeSplit(Bytes.Span, separator, splitOptions, maxCount);
        }

        public SpanRangeSplitEnumerator<byte> EnumerateRangeSplit(
            Utf8String? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None,
            int? maxCount = null)
        {
            return EnumerateRangeSplit(separator.AsSpan(), splitOptions, maxCount);
        }
    }
}
