using System;
using System.Text;

namespace MCServerSharp
{
    public static class EncodingExtensions
    {
        public static byte[] GetBytes(this Encoding encoding, ReadOnlySpan<char> text)
        {
            byte[] buffer = new byte[encoding.GetByteCount(text)];
            _ = encoding.GetBytes(text, buffer);
            return buffer;
        }
    }
}
