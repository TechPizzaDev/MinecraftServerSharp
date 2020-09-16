using System;

namespace MCServerSharp
{
    public static partial class EnumExtensions
    {
        public static VarInt ToVarInt<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            if (num < int.MinValue || num > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (VarInt)num;
        }

        public static VarLong ToVarLong<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            return (VarLong)num;
        }
    }
}
