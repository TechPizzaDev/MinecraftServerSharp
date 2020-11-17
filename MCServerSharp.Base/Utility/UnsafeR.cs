using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.Utility
{
    public static class UnsafeR
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly TTo As<TFrom, TTo>(in TFrom source)
        {
            return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(source));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T AddByteOffset<T>(in T source, IntPtr byteOffset)
        {
            return ref Unsafe.AddByteOffset(ref Unsafe.AsRef(source), byteOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T AddByteOffset<T>(in T source, int byteOffset)
        {
            return ref AddByteOffset(source, (IntPtr)byteOffset);
        }

        public static ReadOnlySpan<T> AsReadOnlySpan<T>(in T value, int count = 1)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(value), count);
        }

        public static Span<T> AsSpan<T>(ref T value, int count = 1)
        {
            return MemoryMarshal.CreateSpan(ref value, count);
        }
    }
}
