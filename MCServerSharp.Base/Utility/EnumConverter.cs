using System;
using System.Linq.Expressions;

namespace MCServerSharp
{
    public static class EnumConverter
    {
        public static long ToInt64<TEnum>(TEnum value)
            where TEnum : Enum
        {
            return Helper<TEnum>.ConvertFrom(value);
        }

        public static ulong ToUInt64<TEnum>(TEnum value)
            where TEnum : Enum
        {
            return (ulong)ToInt64(value);
        }

        public static TEnum ToEnum<TEnum>(long value)
            where TEnum : Enum
        {
            return Helper<TEnum>.ConvertTo(value);
        }

        public static TEnum ToEnum<TEnum>(ulong value)
            where TEnum : Enum
        {
            return ToEnum<TEnum>((long)value);
        }

        private static class Helper<TEnum>
        {
            public static Func<TEnum, long> ConvertFrom { get; } = GenerateFromConverter();
            public static Func<long, TEnum> ConvertTo { get; } = GenerateToConverter();

            private static Func<TEnum, long> GenerateFromConverter()
            {
                var parameter = Expression.Parameter(typeof(TEnum));
                var conversion = Expression.Convert(parameter, typeof(long));
                var method = Expression.Lambda<Func<TEnum, long>>(conversion, parameter);
                return method.Compile();
            }

            private static Func<long, TEnum> GenerateToConverter()
            {
                var parameter = Expression.Parameter(typeof(long));
                var conversion = Expression.Convert(parameter, typeof(TEnum));
                var method = Expression.Lambda<Func<long, TEnum>>(conversion, parameter);
                return method.Compile();
            }
        }
    }
}
