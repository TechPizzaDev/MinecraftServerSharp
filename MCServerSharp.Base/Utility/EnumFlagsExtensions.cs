using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace MCServerSharp
{
    public static class EnumFlagsExtensions
    {
        #region HasFlags

        /// <summary>
        /// Determines whether the value contains the given mask.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlags<TEnum>(this TEnum value, TEnum mask)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
            {
                byte m = As<TEnum, byte>(ref mask);
                return (As<TEnum, byte>(ref value) & m) == m;
            }
            else if (SizeOf<TEnum>() == 2)
            {
                ushort m = As<TEnum, ushort>(ref mask);
                return (As<TEnum, ushort>(ref value) & m) == m;
            }
            else if (SizeOf<TEnum>() == 4)
            {
                uint m = As<TEnum, uint>(ref mask);
                return (As<TEnum, uint>(ref value) & m) == m;
            }
            else if (SizeOf<TEnum>() == 8)
            {
                ulong m = As<TEnum, ulong>(ref mask);
                return (As<TEnum, ulong>(ref value) & m) == m;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        /// <summary>
        /// Determines whether the value contains the sum of given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlags<TEnum>(this TEnum value, ReadOnlySpan<TEnum> masks)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
            {
                byte mask = 0;
                ReadOnlySpan<byte> ms = MemoryMarshal.Cast<TEnum, byte>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, byte>(ref value) & mask) == mask;
            }
            else if (SizeOf<TEnum>() == 2)
            {
                ushort mask = 0;
                ReadOnlySpan<ushort> ms = MemoryMarshal.Cast<TEnum, ushort>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, ushort>(ref value) & mask) == mask;
            }
            else if (SizeOf<TEnum>() == 4)
            {
                uint mask = 0;
                ReadOnlySpan<uint> ms = MemoryMarshal.Cast<TEnum, uint>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, uint>(ref value) & mask) == mask;
            }
            else if (SizeOf<TEnum>() == 8)
            {
                ulong mask = 0;
                ReadOnlySpan<ulong> ms = MemoryMarshal.Cast<TEnum, ulong>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, ulong>(ref value) & mask) == mask;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        #endregion

        #region HasAnyFlag

        /// <summary>
        /// Determines whether the value has any flag of the given mask.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyFlag<TEnum>(this TEnum value, TEnum mask)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
            {
                return (As<TEnum, byte>(ref value) & As<TEnum, byte>(ref mask)) != 0;
            }
            else if (SizeOf<TEnum>() == 2)
            {
                return (As<TEnum, ushort>(ref value) & As<TEnum, ushort>(ref mask)) != 0;
            }
            else if (SizeOf<TEnum>() == 4)
            {
                return (As<TEnum, uint>(ref value) & As<TEnum, uint>(ref mask)) != 0;
            }
            else if (SizeOf<TEnum>() == 8)
            {
                return (As<TEnum, ulong>(ref value) & As<TEnum, ulong>(ref mask)) != 0;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        /// <summary>
        /// Determines whether the value has any flag of the given masks.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyFlag<TEnum>(this TEnum value, ReadOnlySpan<TEnum> masks)
            where TEnum : unmanaged, Enum
        {
            if (SizeOf<TEnum>() == 1)
            {
                byte mask = 0;
                ReadOnlySpan<byte> ms = MemoryMarshal.Cast<TEnum, byte>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, byte>(ref value) & mask) != 0;
            }
            else if (SizeOf<TEnum>() == 2)
            {
                ushort mask = 0;
                ReadOnlySpan<ushort> ms = MemoryMarshal.Cast<TEnum, ushort>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, ushort>(ref value) & mask) != 0;
            }
            else if (SizeOf<TEnum>() == 4)
            {
                uint mask = 0;
                ReadOnlySpan<uint> ms = MemoryMarshal.Cast<TEnum, uint>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, uint>(ref value) & mask) != 0;
            }
            else if (SizeOf<TEnum>() == 8)
            {
                ulong mask = 0;
                ReadOnlySpan<ulong> ms = MemoryMarshal.Cast<TEnum, ulong>(masks);
                for (int i = 0; i < ms.Length; i++)
                {
                    mask |= ms[i];
                }
                return (As<TEnum, ulong>(ref value) & mask) != 0;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        #endregion
    }
}
