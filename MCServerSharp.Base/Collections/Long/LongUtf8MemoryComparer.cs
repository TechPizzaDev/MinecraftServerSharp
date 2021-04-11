
namespace MCServerSharp.Collections
{
    internal sealed class LongUtf8MemoryComparer : LongEqualityComparer<Utf8Memory>
    {
        public override bool IsRandomized => true;

        public override int GetHashCode(Utf8Memory value)
        {
            if (value.IsEmpty)
                return 0;

            var span = value.Span;
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash32(hash);
        }

        public override long GetLongHashCode(Utf8Memory value)
        {
            if (value.IsEmpty)
                return 0;

            var span = value.Span;
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}