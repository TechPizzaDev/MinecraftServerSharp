using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    public class LongDiffusedEqualityComparer<T> : ILongEqualityComparer<T>
    {
        public bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            return LongEqualityComparer<T>.Default.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            int code = LongEqualityComparer<T>.Default.GetHashCode(obj);
            return HashCode.Combine(code);
        }

        public long GetLongHashCode([DisallowNull] T value)
        {
            long code = LongEqualityComparer<T>.Default.GetLongHashCode(value);
            return LongHashCode.Combine(code);
        }
    }
}
