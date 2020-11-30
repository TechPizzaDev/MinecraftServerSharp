using System;

namespace MCServerSharp.Text
{
    public static class Utf8RuneEnumeratorExtensions
    {
        public static Utf8RuneEnumerator GetUtf8Enumerator(this ReadOnlySpan<byte> utf8)
        {
            return new Utf8RuneEnumerator(utf8);
        }
    }
}
