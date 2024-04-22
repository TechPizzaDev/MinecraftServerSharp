using System;

namespace MCServerSharp.Collections
{
    public class BitSet
    {
        private long[] _longs;

        public bool this[int index]
        {
            get => (_longs[index / 64] & (1L << (index % 64))) != 0;
            set
            {
                if (value)
                {
                    _longs[index / 64] |= (1L << (index % 64));
                }
                else
                {
                    _longs[index / 64] &= ~(1L << (index % 64));
                }
            }
        }

        public BitSet(int capacity)
        {
            _longs = new long[(capacity + 63) / 64];
        }

        public Span<long> AsSpan()
        {
            return _longs.AsSpan();
        }
    }
}
