using System.Runtime.CompilerServices;

namespace MCServerSharp.Collections
{
    internal sealed class LongDoubleComparer : LongEqualityComparer<double>
    {
        public override unsafe long GetLongHashCode(double value)
        {
            // Ensure that 0 and -0 have the same hash code
            if (value == 0)
                return 0;

            return Unsafe.As<double, long>(ref value);
        }
    }
}
