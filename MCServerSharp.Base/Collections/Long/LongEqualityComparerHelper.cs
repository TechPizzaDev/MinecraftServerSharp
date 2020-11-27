using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    internal static class LongEqualityComparerHelper
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LongNullableComparer<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LongEnumComparer<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LongHashableComparer<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LongGenericComparer<>))]
        public static object CreateComparer(Type type, bool randomized)
        {
            if (type.IsGenericTypeDefinition &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingType = Nullable.GetUnderlyingType(type)!;
                Type comparerType = typeof(LongNullableComparer<>).MakeGenericType(underlyingType);
                return Activator.CreateInstance(comparerType)!;
            }

            if (type == typeof(string))
            {
                return randomized
                    ? (object)new LongStringComparer()
                    : new NonRandomLongStringComparer();
            }

            if (type == typeof(Utf8String))
            {
                return randomized
                    ? (object)new LongUtf8StringComparer()
                    : new NonRandomLongUtf8StringComparer();
            }

            if (type == typeof(long))
                return new LongInt64Comparer();

            if (type == typeof(ulong))
                return new LongUInt64Comparer();

            if (type == typeof(IntPtr))
                return new LongIntPtrComparer();

            if (type == typeof(UIntPtr))
                return new LongUIntPtrComparer();

            if (type == typeof(double))
                return new LongDoubleComparer();

            if (type == typeof(decimal))
                return new LongDecimalComparer();

            Type comparerDefinition;
            if (type.IsEnum)
            {
                comparerDefinition = typeof(LongEnumComparer<>);
            }
            else if (typeof(ILongHashable).IsAssignableFrom(type))
            {
                comparerDefinition = typeof(LongHashableComparer<>);
            }
            else
            {
                comparerDefinition = typeof(LongGenericComparer<>);
            }
            return Activator.CreateInstance(comparerDefinition.MakeGenericType(type))!;
        }
    }
}