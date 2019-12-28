using System;

namespace MinecraftServerSharp.DataTypes
{
    public static class EnumToVarIntExtensions
    {
        public static VarInt32 ToVarInt32<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            if (num < int.MinValue || num > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (int)num;
        }

        public static VarInt64 ToVarInt64<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            long num = EnumConverter<TEnum>.Convert(value);
            return num;
        }
    }
}
