using System;
using System.Collections.Generic;
using System.Text;

namespace MCServerSharp.Text
{
    public static class RuneEnumeratorExtensions
    {
        public static RuneEnumerator GetRuneEnumerator(this ReadOnlySpan<byte> utf8)
        {
            return new RuneEnumerator(new Utf8RuneEnumerator(utf8));
        }

        public static RuneEnumerator GetRuneEnumerator(this ReadOnlyMemory<byte> utf8)
        {
            return GetRuneEnumerator(utf8.Span);
        }

        public static RuneEnumerator GetRuneEnumerator(this ReadOnlySpan<char> text)
        {
            return text;
        }

        public static RuneEnumerator GetRuneEnumerator(this ReadOnlyMemory<char> text)
        {
            return text;
        }

        public static RuneEnumerator GetRuneEnumerator(this string text)
        {
            return text;
        }

        public static RuneEnumerator GetRuneEnumerator(this Utf8String text)
        {
            return text;
        }

        public static RuneEnumerator GetRuneEnumerator(this StringBuilder text)
        {
            return text;
        }

        public static RuneEnumerator GetRuneEnumerator(this IEnumerator<Rune>? text)
        {
            return new RuneEnumerator(text);
        }

        public static RuneEnumerator GetRuneEnumerator(this IEnumerable<Rune>? text)
        {
            return GetRuneEnumerator(text?.GetEnumerator());
        }

        public static RuneEnumerator GetRuneEnumerator(this IEnumerator<char>? text)
        {
            return new RuneEnumerator(text);
        }

        public static RuneEnumerator GetRuneEnumerator(this IEnumerable<char>? text)
        {
            return GetRuneEnumerator(text?.GetEnumerator());
        }
    }
}
