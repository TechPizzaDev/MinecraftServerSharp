using System;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    internal class LongStringComparer : LongEqualityComparer<string?>
    {
        public override long GetLongHashCode(string? str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            var span = MemoryMarshal.AsBytes(str.AsSpan());
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}