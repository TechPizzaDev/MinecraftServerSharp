using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp
{
    public static class Utf8StringExtensions
    {
        public static ReadOnlySpan<byte> AsSpan(this Utf8String? value)
        {
            if (value == null)
                return default;
            return value.Bytes;
        }

        public static ReadOnlySpan<byte> AsSpan(this Utf8String? value, int start)
        {
            if (value == null)
                return default;
            return value.Bytes[start..];
        }

        public static ReadOnlySpan<byte> AsSpan(this Utf8String? value, int start, int count)
        {
            if (value == null)
                return default;
            return value.Bytes.Slice(start, count);
        }

        [return: NotNullIfNotNull("value")]
        public static Utf8String? ToUtf8String(this string? value)
        {
            if (value == null)
                return null;

            // TODO: string interning

            return new Utf8String(value);
        }
    }
}
