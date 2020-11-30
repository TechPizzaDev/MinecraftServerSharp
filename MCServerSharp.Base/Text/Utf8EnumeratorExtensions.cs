using System.Collections.Generic;
using System.Text;

namespace MCServerSharp.Text
{
    public static class Utf8EnumeratorExtensions
    {
        public static Utf8Enumerator GetUtf8Enumerator(this RuneEnumerator runes)
        {
            return new Utf8Enumerator(runes);
        }

        public static Utf8Enumerator GetUtf8Enumerator(this IEnumerator<Rune>? text)
        {
            return new Utf8Enumerator(text);
        }

        public static Utf8Enumerator GetUtf8Enumerator(this IEnumerable<Rune>? text)
        {
            return GetUtf8Enumerator(text?.GetEnumerator());
        }

        public static Utf8Enumerator GetUtf8Enumerator(this IEnumerator<byte>? text)
        {
            return new Utf8Enumerator(text);
        }

        public static Utf8Enumerator GetUtf8Enumerator(this IEnumerable<byte>? text)
        {
            return GetUtf8Enumerator(text?.GetEnumerator());
        }
    }
}
