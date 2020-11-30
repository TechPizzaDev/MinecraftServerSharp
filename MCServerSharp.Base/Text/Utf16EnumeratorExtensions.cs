using System.Collections.Generic;
using System.Text;

namespace MCServerSharp.Text
{
    public static class Utf16EnumeratorExtensions
    {
        public static Utf16Enumerator GetUtf16Enumerator(this RuneEnumerator runes)
        {
            return new Utf16Enumerator(runes);
        }

        public static Utf16Enumerator GetUtf16Enumerator(this IEnumerator<Rune>? text)
        {
            return new Utf16Enumerator(text);
        }

        public static Utf16Enumerator GetUtf16Enumerator(this IEnumerable<Rune>? text)
        {
            return GetUtf16Enumerator(text?.GetEnumerator());
        }

        public static Utf16Enumerator GetUtf16Enumerator(this IEnumerator<char>? text)
        {
            return new Utf16Enumerator(text);
        }

        public static Utf16Enumerator GetUtf16Enumerator(this IEnumerable<char>? text)
        {
            return GetUtf16Enumerator(text?.GetEnumerator());
        }
    }
}
