// Based on:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Formats/Png/Zlib/Adler32.css
// https://github.com/chromium/chromium/blob/master/third_party/zlib/adler32_simd.c

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MCServerSharp.IO.Compression
{
    public static class Adler32
    {
        // Largest prime smaller than 65536
        public const int BASE = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        public const int NMAX = 5552;

        public const int MinBufferSize = 64;

        public static ReadOnlySpan<byte> Tap1Tap2 => new byte[]
        {
                32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, // tap1
                16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 // tap2
        };

        /// <summary>
        /// Calculates an Adler32 checksum based on a seed.
        /// </summary>
        /// <param name="buffer">The span of bytes.</param>
        /// <param name="seed">The Adler32 seed value.</param>
        /// <returns>The checksum.</returns>
        public static uint Calculate(ReadOnlySpan<byte> buffer, uint seed)
        {
            if (buffer.IsEmpty)
                return seed;

            if (Ssse3.IsSupported)
            {
                if (buffer.Length >= MinBufferSize)
                    return CalculateSse(buffer, seed);
            }

            return CalculateScalar(buffer, seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe uint CalculateSse(ReadOnlySpan<byte> buffer, uint seed)
        {
            uint s1 = seed & 0xFFFF;
            uint s2 = (seed >> 16) & 0xFFFF;

            // Process the data in blocks.
            const int BLOCK_SIZE = 1 << 5;

            uint length = (uint)buffer.Length;
            uint blocks = length / BLOCK_SIZE;
            length -= blocks * BLOCK_SIZE;

            int index = 0;
            fixed (byte* bufferPtr = buffer)
            fixed (byte* tapPtr = Tap1Tap2)
            {
                index += (int)blocks * BLOCK_SIZE;
                var srcPtr = bufferPtr;

                // _mm_setr_epi8 on x86
                Vector128<sbyte> tap1 = Sse2.LoadVector128((sbyte*)tapPtr);
                Vector128<sbyte> tap2 = Sse2.LoadVector128((sbyte*)(tapPtr + 16));
                Vector128<byte> zero = Vector128<byte>.Zero;
                Vector128<short> ones = Vector128.Create((short)1);

                while (blocks > 0)
                {
                    uint n = NMAX / BLOCK_SIZE;  /* The NMAX constraint. */
                    if (n > blocks)
                        n = blocks;

                    blocks -= n;

                    // Process n blocks of data. At most NMAX data bytes can be
                    // processed before s2 must be reduced modulo BASE.
                    Vector128<uint> v_ps = Vector128.CreateScalar(s1 * n);
                    Vector128<uint> v_s2 = Vector128.CreateScalar(s2);
                    Vector128<uint> v_s1 = Vector128<uint>.Zero;

                    do
                    {
                        // Load 32 input bytes.
                        Vector128<byte> bytes1 = Sse3.LoadDquVector128(srcPtr);
                        Vector128<byte> bytes2 = Sse3.LoadDquVector128(srcPtr + 0x10);

                        // Add previous block byte sum to v_ps.
                        v_ps = Sse2.Add(v_ps, v_s1);

                        // Horizontally add the bytes for s1, multiply-adds the
                        // bytes by [ 32, 31, 30, ... ] for s2.
                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                        Vector128<short> mad1 = Ssse3.MultiplyAddAdjacent(bytes1, tap1);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad1, ones).AsUInt32());

                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes2, zero).AsUInt32());
                        Vector128<short> mad2 = Ssse3.MultiplyAddAdjacent(bytes2, tap2);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad2, ones).AsUInt32());

                        srcPtr += BLOCK_SIZE;
                    }
                    while (--n > 0);

                    v_s2 = Sse2.Add(v_s2, Sse2.ShiftLeftLogical(v_ps, 5));

                    // Sum epi32 ints v_s1(s2) and accumulate in s1(s2).
                    const byte S2301 = 0b1011_0001;  // A B C D -> B A D C
                    const byte S1032 = 0b0100_1110;  // A B C D -> C D A B

                    v_s1 = Sse2.Add(v_s1, Sse2.Shuffle(v_s1, S1032));
                    s1 += v_s1.ToScalar();

                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S2301));
                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S1032));
                    s2 = v_s2.ToScalar();

                    // Reduce.
                    s1 %= BASE;
                    s2 %= BASE;
                }

                if (length > 0)
                {
                    if (length >= 16)
                    {
                        s2 += s1 += srcPtr[0];
                        s2 += s1 += srcPtr[1];
                        s2 += s1 += srcPtr[2];
                        s2 += s1 += srcPtr[3];
                        s2 += s1 += srcPtr[4];
                        s2 += s1 += srcPtr[5];
                        s2 += s1 += srcPtr[6];
                        s2 += s1 += srcPtr[7];
                        s2 += s1 += srcPtr[8];
                        s2 += s1 += srcPtr[9];
                        s2 += s1 += srcPtr[10];
                        s2 += s1 += srcPtr[11];
                        s2 += s1 += srcPtr[12];
                        s2 += s1 += srcPtr[13];
                        s2 += s1 += srcPtr[14];
                        s2 += s1 += srcPtr[15];

                        srcPtr += 16;
                        length -= 16;
                    }

                    while (length-- > 0)
                        s2 += s1 += *srcPtr++;

                    if (s1 >= BASE)
                        s1 -= BASE;

                    s2 %= BASE;
                }

                return s1 | (s2 << 16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe uint CalculateScalar(ReadOnlySpan<byte> buffer, uint seed)
        {
            uint s1 = seed & 0xFFFF;
            uint s2 = (seed >> 16) & 0xFFFF;
            int k;

            fixed (byte* bufferPtr = buffer)
            {
                var src = bufferPtr;
                int length = buffer.Length;

                while (length > 0)
                {
                    k = length < NMAX ? length : NMAX;
                    length -= k;

                    while (k >= 16)
                    {
                        s2 += s1 += src[0];
                        s2 += s1 += src[1];
                        s2 += s1 += src[2];
                        s2 += s1 += src[3];
                        s2 += s1 += src[4];
                        s2 += s1 += src[5];
                        s2 += s1 += src[6];
                        s2 += s1 += src[7];
                        s2 += s1 += src[8];
                        s2 += s1 += src[9];
                        s2 += s1 += src[10];
                        s2 += s1 += src[11];
                        s2 += s1 += src[12];
                        s2 += s1 += src[13];
                        s2 += s1 += src[14];
                        s2 += s1 += src[15];

                        src += 16;
                        k -= 16;
                    }

                    while (k-- > 0)
                        s2 += s1 += *src++;

                    s1 %= BASE;
                    s2 %= BASE;
                }

                return (s2 << 16) | s1;
            }
        }
    }
}