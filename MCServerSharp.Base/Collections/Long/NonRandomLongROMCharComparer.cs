using System;

namespace MCServerSharp.Collections
{
    /// <summary>
    /// Fast comparer but can create many hash collisions if
    /// values are specially crafted by attackers.
    /// </summary>
    /// <remarks>
    /// Use this if and only if 'Denial of Service' attacks are not a concern 
    /// (i.e. never used for free-form user input),
    /// or are otherwise mitigated.
    /// </remarks>
    public sealed class NonRandomLongROMCharComparer : LongEqualityComparer<ReadOnlyMemory<char>>
    {
        public override bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return x.Span.Equals(y.Span, StringComparison.Ordinal);
        }

        public override int GetHashCode(ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty)
                return 0;

            var (h1, h2) = NonRandomLongStringComparer.Hash(value.Span);
            return (int)(h1 + (h2 * 1566083941));
        }

        public override long GetLongHashCode(ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty)
                return 0;

            var (h1, h2) = NonRandomLongStringComparer.Hash(value.Span);
            return (long)(h1 | (ulong)h2 << 32);
        }
    }
}
