// Copied from .NET Foundation (and Modified)

using System;

namespace MCServerSharp.Collections
{
    public partial class LongHashSet<T>
    {
        internal ref struct BitHelper
        {
            private const int IntBitCount = sizeof(int) * 8;
            private readonly Span<int> _span;

            public BitHelper(Span<int> span, bool clear)
            {
                if (clear)
                    span.Clear();
                _span = span;
            }

            public void MarkBit(int bitPosition)
            {
                int bitArrayIndex = bitPosition / IntBitCount;
                if ((uint)bitArrayIndex < (uint)_span.Length)
                    _span[bitArrayIndex] |= 1 << (bitPosition % IntBitCount);
            }

            public bool IsMarked(int bitPosition)
            {
                int bitArrayIndex = bitPosition / IntBitCount;
                return (uint)bitArrayIndex < (uint)_span.Length
                    && (_span[bitArrayIndex] & (1 << (bitPosition % IntBitCount))) != 0;
            }

            /// <summary>
            /// How many ints must be allocated to represent n bits. Returns (n+31)/32, but avoids overflow.
            /// </summary>
            public static int ToIntArrayLength(int n)
            {
                return n > 0 ? ((n - 1) / IntBitCount + 1) : 0;
            }
        }
    }
}
