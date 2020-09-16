using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCServerSharp.Collections
{
    internal class LongNullableComparer<T> : LongEqualityComparer<T?>
        where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue) 
                    return x.Value.Equals(y.Value);
                return false;
            }
            if (y.HasValue)
                return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T? obj)
        {
            return obj.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long GetLongHashCode([DisallowNull] T? value)
        {
            return value != null ? LongEqualityComparer<T>.Default.GetLongHashCode(value.Value) : 0;
        }
    }
}