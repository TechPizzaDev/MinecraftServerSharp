using System;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    internal sealed class LongROMCharComparer : LongEqualityComparer<ReadOnlyMemory<char>>
    {
        public override bool IsRandomized => true;

        public override bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return x.Span.Equals(y.Span, StringComparison.Ordinal);
        }

        public override int GetHashCode(ReadOnlyMemory<char> value)
        {
            var span = value.Span;
            if (span.IsEmpty)
                return 0;

            var bytes = MemoryMarshal.AsBytes(span);
            var hash = MarvinHash64.ComputeHash(bytes, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash32(hash);
        }
        
        public override long GetLongHashCode(ReadOnlyMemory<char> value)
        {
            var span = value.Span;
            if (span.IsEmpty)
                return 0;

            var bytes = MemoryMarshal.AsBytes(span);
            var hash = MarvinHash64.ComputeHash(bytes, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}