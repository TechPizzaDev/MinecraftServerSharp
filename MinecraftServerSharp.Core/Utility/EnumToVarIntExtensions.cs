using System;

namespace MinecraftServerSharp.DataTypes
{
    public static class EnumToVarIntExtensions
    {
        public static VarInt ToVarInt32<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            if (num < int.MinValue || num > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (VarInt)num;
        }

        public static VarLong ToVarInt64<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            return (VarLong)num;
        }
    }
}
