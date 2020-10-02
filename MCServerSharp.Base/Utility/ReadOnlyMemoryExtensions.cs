using System;

namespace MCServerSharp
{
    public static class ReadOnlyMemoryExtensions
    {
        public static Utf8String ToUtf8String(this ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty)
                return Utf8String.Empty;

            return new Utf8String(bytes.Span);
        }
    }
}
