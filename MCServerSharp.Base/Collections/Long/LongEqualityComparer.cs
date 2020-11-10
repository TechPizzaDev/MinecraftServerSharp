using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCServerSharp.Collections
{
    public abstract class LongEqualityComparer<T> : EqualityComparer<T>, ILongEqualityComparer<T>
    {
        public static new LongEqualityComparer<T> Default { get; } =
            (LongEqualityComparer<T>)LongEqualityComparerHelper.CreateComparer(typeof(T), randomized: true);

        public static LongEqualityComparer<T> NonRandomDefault { get; } =
            (LongEqualityComparer<T>)LongEqualityComparerHelper.CreateComparer(typeof(T), randomized: false);

        public virtual bool IsRandomized => false;

        public LongEqualityComparer()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] T value)
        {
            return EqualityComparer<T>.Default.GetHashCode(value);
        }

        public abstract long GetLongHashCode([DisallowNull] T value);
    }
}