using System;
using System.Linq.Expressions;

namespace MCServerSharp
{
    public static partial class EnumConverter<TEnum>
        where TEnum : Enum
    {
        private static Func<long, TEnum> ConvertTo { get; } = GenerateToConverter();
        private static Func<TEnum, long> ConvertFrom { get; } = GenerateFromConverter();

        public static TEnum Convert(long value) => ConvertTo.Invoke(value);

        public static TEnum Convert(ulong value) => Convert((long)value);

        public static long Convert(TEnum value) => ConvertFrom.Invoke(value);

        private static Func<long, TEnum> GenerateToConverter()
        {
            var parameter = Expression.Parameter(typeof(long));
            var conversion = Expression.Convert(parameter, typeof(TEnum));
            var method = Expression.Lambda<Func<long, TEnum>>(conversion, parameter);
            return method.Compile();
        }

        private static Func<TEnum, long> GenerateFromConverter()
        {
            var parameter = Expression.Parameter(typeof(TEnum));
            var conversion = Expression.Convert(parameter, typeof(long));
            var method = Expression.Lambda<Func<TEnum, long>>(conversion, parameter);
            return method.Compile();
        }
    }
}
