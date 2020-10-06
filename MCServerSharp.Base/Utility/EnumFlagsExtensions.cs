using System;
using System.Linq.Expressions;

namespace MCServerSharp
{
    public static class EnumFlagsExtensions
    {
        #region HasFlags

        /// <summary>
        /// Determines whether the value is equal to the given mask.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(this TEnum value, TEnum mask) where TEnum : Enum
        {
            return HasFlagsHelper<TEnum>.Func(value, mask);
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1) where TEnum : Enum
        {
            if (HasFlagsHelper<TEnum>.Func(value, mask0) ||
                HasFlagsHelper<TEnum>.Func(value, mask1))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2) where TEnum : Enum
        {
            if (HasFlagsHelper<TEnum>.Func(value, mask0) ||
                HasFlagsHelper<TEnum>.Func(value, mask1) ||
                HasFlagsHelper<TEnum>.Func(value, mask2))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3) where TEnum : Enum
        {
            if (HasFlagsHelper<TEnum>.Func(value, mask0) ||
                HasFlagsHelper<TEnum>.Func(value, mask1) ||
                HasFlagsHelper<TEnum>.Func(value, mask2) ||
                HasFlagsHelper<TEnum>.Func(value, mask3))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3, TEnum mask4) where TEnum : Enum
        {
            if (HasFlagsHelper<TEnum>.Func(value, mask0) ||
                HasFlagsHelper<TEnum>.Func(value, mask1) ||
                HasFlagsHelper<TEnum>.Func(value, mask2) ||
                HasFlagsHelper<TEnum>.Func(value, mask3) ||
                HasFlagsHelper<TEnum>.Func(value, mask4))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3, TEnum mask4, TEnum mask5) where TEnum : Enum
        {
            if (HasFlagsHelper<TEnum>.Func(value, mask0) ||
                HasFlagsHelper<TEnum>.Func(value, mask1) ||
                HasFlagsHelper<TEnum>.Func(value, mask2) ||
                HasFlagsHelper<TEnum>.Func(value, mask3) ||
                HasFlagsHelper<TEnum>.Func(value, mask4) ||
                HasFlagsHelper<TEnum>.Func(value, mask5))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(this TEnum value, ReadOnlySpan<TEnum> masks) where TEnum : Enum
        {
            for (int i = 0; i < masks.Length; i++)
                if (HasFlagsHelper<TEnum>.Func(value, masks[i]))
                    return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value is equal to any of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasFlags<TEnum>(this TEnum value, params TEnum[] masks) where TEnum : Enum
        {
            return HasFlags(value, masks);
        }

        #endregion

        #region HasAnyFlag

        /// <summary>
        /// Determines whether the value has any flag of the given mask.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(this TEnum value, TEnum mask) where TEnum : Enum
        {
            return HasAnyFlagHelper<TEnum>.Func(value, mask);
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1) where TEnum : Enum
        {
            if (HasAnyFlagHelper<TEnum>.Func(value, mask0) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask1))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2) where TEnum : Enum
        {
            if (HasAnyFlagHelper<TEnum>.Func(value, mask0) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask1) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask2))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3) where TEnum : Enum
        {
            if (HasAnyFlagHelper<TEnum>.Func(value, mask0) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask1) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask2) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask3))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3, TEnum mask4) where TEnum : Enum
        {
            if (HasAnyFlagHelper<TEnum>.Func(value, mask0) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask1) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask2) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask3) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask4))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(
            this TEnum value, TEnum mask0, TEnum mask1, TEnum mask2, TEnum mask3, TEnum mask4, TEnum mask5) where TEnum : Enum
        {
            if (HasAnyFlagHelper<TEnum>.Func(value, mask0) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask1) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask2) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask3) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask4) ||
                HasAnyFlagHelper<TEnum>.Func(value, mask5))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(this TEnum value, ReadOnlySpan<TEnum> masks) where TEnum : Enum
        {
            for (int i = 0; i < masks.Length; i++)
                if (HasAnyFlagHelper<TEnum>.Func(value, masks[i]))
                    return true;
            return false;
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        public static bool HasAnyFlag<TEnum>(this TEnum value, params TEnum[] masks) where TEnum : Enum
        {
            return HasFlags(value, masks);
        }


        #endregion

        public static class HasFlagsHelper<TEnum> where TEnum : Enum
        {
            public static Func<TEnum, TEnum, bool> Func { get; } = CreateFunc();

            private static Func<TEnum, TEnum, bool> CreateFunc()
            {
                ParameterExpression valueExpression = Expression.Parameter(typeof(TEnum));
                ParameterExpression flagExpression = Expression.Parameter(typeof(TEnum));
                ParameterExpression flagValueVariable = Expression.Variable(
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

        public static class HasAnyFlagHelper<TEnum> where TEnum : Enum
        {
            public static Func<TEnum, TEnum, bool> Func { get; } = CreateFunc();

            private static Func<TEnum, TEnum, bool> CreateFunc()
            {
                ParameterExpression valueExpression = Expression.Parameter(typeof(TEnum));
                ParameterExpression flagExpression = Expression.Parameter(typeof(TEnum));
                Type flagValueType = Type.GetTypeCode(typeof(TEnum)) == TypeCode.UInt64 ? typeof(ulong) : typeof(long);

                var body = Expression.NotEqual(
                    Expression.And(
                        Expression.Convert(valueExpression, flagValueType),
                        Expression.Convert(flagExpression, flagValueType)),
                    Expression.Default(flagValueType));

                var lambda = Expression.Lambda<Func<TEnum, TEnum, bool>>(
                    body, valueExpression, flagExpression);

                return lambda.Compile();
            }
        }
    }
}
