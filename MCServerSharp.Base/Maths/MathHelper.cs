using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp
{
    public static class MathHelper
    {
        private static ReadOnlySpan<byte> MultiplyDeBruijnBitPosition => new byte[]
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        public static uint SmallestPowerOfTwo(uint value)
        {
            uint i = value - 1;
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }

        public static bool IsPowerOfTwo(uint value)
        {
            return value != 0 && (value & value - 1) == 0;
        }

        public static int Log2Ceil(uint value)
        {
            value = IsPowerOfTwo(value) ? value : SmallestPowerOfTwo(value);

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                ref MemoryMarshal.GetReference(MultiplyDeBruijnBitPosition),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x77CB531u) >> 27));
        }

        public static int Log2Ceil(int value)
        {
            return Log2Ceil((uint)value);
        }
    }
}
