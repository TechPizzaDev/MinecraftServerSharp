using System;

namespace MCServerSharp
{
    public static class ReadOnlySpanExtensions
    {
        public static Utf8String ToUtf8String(this ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return Utf8String.Empty;

            return new Utf8String(bytes);
        }
    }
}
