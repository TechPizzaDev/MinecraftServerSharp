using System;

namespace MCServerSharp
{
    public static class VarIntExtensions
    {
        public static TEnum AsEnum<TEnum>(this VarInt value)
            where TEnum : unmanaged, Enum
        {
            return EnumConverter.ToEnum<TEnum>(value);
        }

        public static TEnum AsEnum<TEnum>(this VarLong value)
            where TEnum : unmanaged, Enum
        {
            return EnumConverter.ToEnum<TEnum>(value);
        }
    }
}
