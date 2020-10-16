using System.Runtime.CompilerServices;

namespace MCServerSharp.Collections
{
    internal sealed class LongDecimalComparer : LongEqualityComparer<decimal>
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override long GetLongHashCode(decimal d)
        {
            ref long data = ref Unsafe.As<decimal, long>(ref d);
            long h1 = data;
            long h2 = Unsafe.Add(ref data, 1);
            return LongHashCode.Combine(h1, h2);
        }
    }
}
