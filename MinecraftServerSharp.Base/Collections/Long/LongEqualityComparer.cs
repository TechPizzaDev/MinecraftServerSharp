using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCServerSharp.Collections
{
    public abstract partial class LongEqualityComparer<T> : EqualityComparer<T>, ILongEqualityComparer<T>
    {
        public static new LongEqualityComparer<T> Default { get; } = 
            (LongEqualityComparer<T>)CreateComparer();

        public LongEqualityComparer()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([AllowNull] T x, [AllowNull] T y) => EqualityComparer<T>.Default.Equals(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] T value) => EqualityComparer<T>.Default.GetHashCode(value);

        public abstract long GetLongHashCode([DisallowNull] T value);

        private static ILongEqualityComparer<T> CreateComparer()
        {
            if (typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingType = Nullable.GetUnderlyingType(typeof(T))!;
                Type comparerType = typeof(LongNullableComparer<>).MakeGenericType(underlyingType);
                return (LongEqualityComparer<T>)Activator.CreateInstance(comparerType)!;
            }

            if (typeof(T) == typeof(string))
                // LongStringComparer is "randomized" by default
                return (ILongEqualityComparer<T>)new LongStringComparer();

            if (typeof(T) == typeof(long))
                return (ILongEqualityComparer<T>)new LongInt64Comparer();

            if (typeof(T) == typeof(ulong))
                return (ILongEqualityComparer<T>)new LongUInt64Comparer();

            if (typeof(T) == typeof(IntPtr))
                return (ILongEqualityComparer<T>)new LongIntPtrComparer();

            if (typeof(T) == typeof(UIntPtr))
                return (ILongEqualityComparer<T>)new LongUIntPtrComparer();

            if (typeof(T) == typeof(double))
                return (ILongEqualityComparer<T>)new LongDoubleComparer();

            if (typeof(T) == typeof(decimal))
                return (ILongEqualityComparer<T>)new LongDecimalComparer();

            if (typeof(ILongHashable).IsAssignableFrom(typeof(T)))
            {
                Type comparerType = typeof(LongHashableComparer<>).MakeGenericType(typeof(T));
                return (LongEqualityComparer<T>)Activator.CreateInstance(comparerType)!;
            }

            return new LongGenericComparer<T>();
        }
    }
}