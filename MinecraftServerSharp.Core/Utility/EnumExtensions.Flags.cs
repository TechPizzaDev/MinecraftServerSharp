using System;
using System.Linq.Expressions;

namespace MinecraftServerSharp
{
    public static partial class EnumExtensions
    {
        /// <summary>
        /// Determines whether a value has the specified flags.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="flags">The flag.</param>
        /// <returns>
        ///  <see langword="true"/> if the specified value has flags; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool HasFlags<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum
        {
            return EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flags);
        }

        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum flag0, TEnum flag1) where TEnum : Enum
        {
            if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag0) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag1))
                return true;
            return false;
        }

        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum flag0, TEnum flag1, TEnum flag2) where TEnum : Enum
        {
            if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag0) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag1) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag2))
                return true;
            return false;
        }

        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum flag0, TEnum flag1, TEnum flag2, TEnum flag3) where TEnum : Enum
        {
            if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag0) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag1) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag2) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag3))
                return true;
            return false;
        }

        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum flag0, TEnum flag1, TEnum flag2, TEnum flag3, TEnum flag4) where TEnum : Enum
        {
            if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag0) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag1) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag2) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag3) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag4))
                return true;
            return false;
        }

        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum flag0, TEnum flag1, TEnum flag2, TEnum flag3, TEnum flag4, TEnum flag5) where TEnum : Enum
        {
            if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag0) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag1) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag2) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag3) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag4) ||
                EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flag5))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether a value has any of the given flags.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="value"></param>
        /// <param name="flags"></param>
        /// <returns><see langword="true"/> if the specified value has any flag; otherwise, <see langword="false"/>.</returns>
        public static bool HasFlags<TEnum>(this TEnum value, ReadOnlySpan<TEnum> flags) where TEnum : Enum
        {
            for (int i = 0; i < flags.Length; i++)
                if (EnumExtensionsFlags<TEnum>.HasFlagsDelegate(value, flags[i]))
                    return true;
            return false;
        }

        /// <summary>
        /// Determines whether a value has any of the given flags.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="value"></param>
        /// <param name="flags"></param>
        /// <returns><see langword="true"/> if the specified value has any flag; otherwise, <see langword="false"/>.</returns>
        public static bool HasFlags<TEnum>(this TEnum value, params TEnum[] flags) where TEnum : Enum
        {
            return HasFlags(value, flags);
        }

        private static class EnumExtensionsFlags<TEnum> where TEnum : Enum
        {
            public static readonly Func<TEnum, TEnum, bool> HasFlagsDelegate = CreateHasFlagDelegate();

            private static Func<TEnum, TEnum, bool> CreateHasFlagDelegate()
            {
                var valueExpression = Expression.Parameter(typeof(TEnum));
                var flagExpression = Expression.Parameter(typeof(TEnum));
                var flagValueVariable = Expression.Variable(
                    Type.GetTypeCode(typeof(TEnum)) == TypeCode.UInt64 ? typeof(ulong) : typeof(long));

                var body = Expression.Block(
                    new[] { flagValueVariable },

                    Expression.Assign(
                        flagValueVariable,
                        Expression.Convert(
                            flagExpression, flagValueVariable.Type)),

                    Expression.Equal(
                        Expression.And(
                            Expression.Convert(
                                valueExpression, flagValueVariable.Type),
                            flagValueVariable),
                        flagValueVariable)
                );

                var lambda = Expression.Lambda<Func<TEnum, TEnum, bool>>(
                    body, valueExpression, flagExpression);

                return lambda.Compile();
            }
        }
    }
}
