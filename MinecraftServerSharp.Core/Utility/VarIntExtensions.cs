using System;

namespace MinecraftServerSharp.DataTypes
{
    public static class VarIntExtensions
    {
        public static TEnum AsEnum<TEnum>(this VarInt32 value)
            where TEnum : Enum
        {
            return EnumConverter<TEnum>.Convert(value);
        }

        public static TEnum AsEnum<TEnum>(this VarInt64 value)
            where TEnum : Enum
        {
            return EnumConverter<TEnum>.Convert(value);
        }
    }
}
