using System;

namespace MCServerSharp
{
    public static class VarIntExtensions
    {
        public static TEnum AsEnum<TEnum>(this VarInt value)
            where TEnum : Enum
        {
            return EnumConverter<TEnum>.Convert(value);
        }

        public static TEnum AsEnum<TEnum>(this VarLong value)
            where TEnum : Enum
        {
            return EnumConverter<TEnum>.Convert(value);
        }
    }
}
