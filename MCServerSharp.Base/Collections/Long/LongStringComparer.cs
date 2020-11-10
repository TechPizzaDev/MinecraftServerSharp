using System;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    internal sealed class LongStringComparer : LongEqualityComparer<string?>
    {
        public override bool IsRandomized => true;

        public override int GetHashCode(string? value)
        {
            var span = value.AsSpan();
            if (span.IsEmpty)
                return 0;

            var bytes = MemoryMarshal.AsBytes(span);
            var hash = MarvinHash64.ComputeHash(bytes, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash32(hash);
        }
        
        public override long GetLongHashCode(string? value)
        {
            var span = value.AsSpan();
            if (span.IsEmpty)
                return 0;

            var bytes = MemoryMarshal.AsBytes(span);
            var hash = MarvinHash64.ComputeHash(bytes, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}