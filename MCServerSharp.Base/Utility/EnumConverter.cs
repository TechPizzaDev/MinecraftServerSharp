using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace MCServerSharp
{
    public static class EnumConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64<TEnum>(TEnum value)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
                return As<TEnum, sbyte>(ref value);
            else if (SizeOf<TEnum>() == 2)
                return As<TEnum, short>(ref value);
            else if (SizeOf<TEnum>() == 4)
                return As<TEnum, int>(ref value);
            else if (SizeOf<TEnum>() == 8)
                return As<TEnum, long>(ref value);
            else
                throw new InvalidCastException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64<TEnum>(TEnum value)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
                return As<TEnum, byte>(ref value);
            else if (SizeOf<TEnum>() == 2)
                return As<TEnum, ushort>(ref value);
            else if (SizeOf<TEnum>() == 4)
                return As<TEnum, uint>(ref value);
            else if (SizeOf<TEnum>() == 8)
                return As<TEnum, ulong>(ref value);
            else
                throw new InvalidCastException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum ToEnum<TEnum>(long value)
            where TEnum : unmanaged, Enum
        {
            return As<long, TEnum>(ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum ToEnum<TEnum>(ulong value)
            where TEnum : unmanaged, Enum
        {
            return As<ulong, TEnum>(ref value);
        }
    }
}
