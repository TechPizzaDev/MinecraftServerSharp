
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
    public sealed class NonRandomLongUtf8MemoryComparer : LongEqualityComparer<Utf8Memory>
    {
        public override int GetHashCode(Utf8Memory value)
        {
            var (h1, h2) = NonRandomLongUtf8StringComparer.Hash(value.Span);
            return (int)(h1 + (h2 * 1566083941));
        }

        public override long GetLongHashCode(Utf8Memory value)
        {
            var (h1, h2) = NonRandomLongUtf8StringComparer.Hash(value.Span);
            return (long)((ulong)h2 << 32 | h1);
        }
    }
}
