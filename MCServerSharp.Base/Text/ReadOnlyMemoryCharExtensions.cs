using System;

namespace MCServerSharp
{
    public static class ReadOnlyMemoryCharExtensions
    {
        public static Utf16Splitter EnumerateSplit(
            this ReadOnlySpan<char> value, 
            ReadOnlySpan<char> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return new Utf16Splitter(value, separator, splitOptions);
        }

        public static Utf16Splitter EnumerateSplit(
            this ReadOnlySpan<char> value,
            ReadOnlyMemory<char> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(value, separator.Span, splitOptions);
        }

        public static Utf16Splitter EnumerateSplit(
            this ReadOnlySpan<char> value,
            string? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(value, separator.AsSpan(), splitOptions);
        }

        public static Utf16Splitter EnumerateSplit(
           this ReadOnlyMemory<char> value,
           ReadOnlySpan<char> separator,
           StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(value.Span, separator, splitOptions);
        }

        public static Utf16Splitter EnumerateSplit(
            this ReadOnlyMemory<char> value,
            ReadOnlyMemory<char> separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(value, separator.Span, splitOptions);
        }

        public static Utf16Splitter EnumerateSplit(
            this ReadOnlyMemory<char> value,
            string? separator,
            StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return EnumerateSplit(value, separator.AsSpan(), splitOptions);
        }
    }
}
