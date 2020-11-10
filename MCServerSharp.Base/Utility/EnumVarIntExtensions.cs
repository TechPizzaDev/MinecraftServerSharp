using System;

namespace MCServerSharp
{
    public static class EnumVarIntExtensions
    {
        public static VarInt ToVarInt<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter.ToInt64(value);
            if (num < int.MinValue || num > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (VarInt)num;
        }

        public static VarLong ToVarLong<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter.ToInt64(value);
            return (VarLong)num;
        }
    }
}
