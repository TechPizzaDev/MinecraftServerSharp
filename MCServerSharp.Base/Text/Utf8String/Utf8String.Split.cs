using System;

namespace MCServerSharp
{
    public partial class Utf8String
    {
        public Utf8Splitter EnumerateSplit(
            ReadOnlySpan<byte> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return new Utf8Splitter(Bytes, separator, splitOptions);
        }

        public Utf8Splitter EnumerateSplit(
            Utf8String? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(separator.AsSpan(), splitOptions);
        }
    }
}
