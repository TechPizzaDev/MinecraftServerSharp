
namespace MCServerSharp.Collections
{
    internal sealed class LongUtf8StringComparer : LongEqualityComparer<Utf8String?>
    {
        public override bool IsRandomized => true;

        public override int GetHashCode(Utf8String? value)
        {
            if (Utf8String.IsNullOrEmpty(value))
                return 0;

            var span = value.AsSpan();
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash32(hash);
        }

        public override long GetLongHashCode(Utf8String? value)
        {
            if (Utf8String.IsNullOrEmpty(value))
                return 0;

            var span = value.AsSpan();
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}