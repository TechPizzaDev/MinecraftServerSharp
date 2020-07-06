using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MinecraftServerSharp.Collections
{
    internal class LongGenericComparer<T> : LongEqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            if (x != null)
            {
                if (y != null) 
                    return x.Equals(y);
                return false;
            }
            if (y != null) 
                return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] T value)
        {
            return value?.GetHashCode() ?? 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long GetLongHashCode([DisallowNull] T value)
        {
            return value?.GetHashCode() ?? 0;
        }
    }
}