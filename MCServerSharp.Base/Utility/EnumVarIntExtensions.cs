using System;
using System.Runtime.CompilerServices;

namespace MCServerSharp
{
    public static class EnumVarIntExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VarInt ToVarInt<TEnum>(this TEnum value)
            where TEnum : unmanaged, Enum
        {
            long num = EnumConverter.ToInt64(value);
            return (VarInt)num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VarLong ToVarLong<TEnum>(this TEnum value)
            where TEnum : unmanaged, Enum
        {
            long num = EnumConverter.ToInt64(value);
            return num;
        }
    }
}
