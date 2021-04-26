using System.Runtime.CompilerServices;

namespace MCServerSharp
{
    public readonly partial struct BitArray32
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void BatchCopy1(uint iterations, ref ulong src, ref uint dst, ref int dstIndex)
        {
            const uint mask = ~(uint.MaxValue << 1);

            for (uint j = 0; j < iterations; j++)
            {
                ulong data = Unsafe.Add(ref src, (int)j);

                for (int i = 0; i < 2; i++)
                {
                    ref uint d = ref Unsafe.Add(ref dst, dstIndex);
                    uint sdata = (uint)(data >> (i * 32));

                    uint element0 = (sdata >> (0)) & mask;
                    uint element1 = (sdata >> (1)) & mask;
                    Unsafe.Add(ref d, 0) = element0;
                    Unsafe.Add(ref d, 1) = element1;

                    uint element2 = (sdata >> (2)) & mask;
                    uint element3 = (sdata >> (3)) & mask;
                    Unsafe.Add(ref d, 2) = element2;
                    Unsafe.Add(ref d, 3) = element3;

                    uint element4 = (sdata >> (4)) & mask;
                    uint element5 = (sdata >> (5)) & mask;
                    Unsafe.Add(ref d, 4) = element4;
                    Unsafe.Add(ref d, 5) = element5;

                    uint element6 = (sdata >> (6)) & mask;
                    uint element7 = (sdata >> (7)) & mask;
                    Unsafe.Add(ref d, 6) = element6;
                    Unsafe.Add(ref d, 7) = element7;

                    uint element8 = (sdata >> (8)) & mask;
                    uint element9 = (sdata >> (9)) & mask;
                    Unsafe.Add(ref d, 8) = element8;
                    Unsafe.Add(ref d, 9) = element9;

                    uint element10 = (sdata >> (10)) & mask;
                    uint element11 = (sdata >> (11)) & mask;
                    Unsafe.Add(ref d, 10) = element10;
                    Unsafe.Add(ref d, 11) = element11;

                    uint element12 = (sdata >> (12)) & mask;
                    uint element13 = (sdata >> (13)) & mask;
                    Unsafe.Add(ref d, 12) = element12;
                    Unsafe.Add(ref d, 13) = element13;

                    uint element14 = (sdata >> (14)) & mask;
                    uint element15 = (sdata >> (15)) & mask;
                    Unsafe.Add(ref d, 14) = element14;
                    Unsafe.Add(ref d, 15) = element15;

                    uint element16 = (sdata >> (16)) & mask;
                    uint element17 = (sdata >> (17)) & mask;
                    Unsafe.Add(ref d, 16) = element16;
                    Unsafe.Add(ref d, 17) = element17;

                    uint element18 = (sdata >> (18)) & mask;
                    uint element19 = (sdata >> (19)) & mask;
                    Unsafe.Add(ref d, 18) = element18;
                    Unsafe.Add(ref d, 19) = element19;

                    uint element20 = (sdata >> (20)) & mask;
                    uint element21 = (sdata >> (21)) & mask;
                    Unsafe.Add(ref d, 20) = element20;
                    Unsafe.Add(ref d, 21) = element21;

                    uint element22 = (sdata >> (22)) & mask;
                    uint element23 = (sdata >> (23)) & mask;
                    Unsafe.Add(ref d, 22) = element22;
                    Unsafe.Add(ref d, 23) = element23;

                    uint element24 = (sdata >> (24)) & mask;
                    uint element25 = (sdata >> (25)) & mask;
                    Unsafe.Add(ref d, 24) = element24;
                    Unsafe.Add(ref d, 25) = element25;

                    uint element26 = (sdata >> (26)) & mask;
                    uint element27 = (sdata >> (27)) & mask;
                    Unsafe.Add(ref d, 26) = element26;
                    Unsafe.Add(ref d, 27) = element27;

                    uint element28 = (sdata >> (28)) & mask;
                    uint element29 = (sdata >> (29)) & mask;
                    Unsafe.Add(ref d, 28) = element28;
                    Unsafe.Add(ref d, 29) = element29;

                    uint element30 = (sdata >> (30)) & mask;
                    uint element31 = (sdata >> (31)) & mask;
                    Unsafe.Add(ref d, 30) = element30;
                    Unsafe.Add(ref d, 31) = element31;

                    dstIndex += 32;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void BatchCopy2(uint iterations, ref ulong src, ref uint dst, ref int dstIndex)
        {
            const uint mask = ~(uint.MaxValue << 2);

            for (uint j = 0; j < iterations; j++)
            {
                ulong data = Unsafe.Add(ref src, (int)j);

                for (int i = 0; i < 2; i++)
                {
                    ref uint d = ref Unsafe.Add(ref dst, dstIndex);
                    uint sdata = (uint)(data >> (i * 32));

                    uint element0 = (sdata >> (0 * 2)) & mask;
                    uint element1 = (sdata >> (1 * 2)) & mask;
                    Unsafe.Add(ref d, 0) = element0;
                    Unsafe.Add(ref d, 1) = element1;

                    uint element2 = (sdata >> (2 * 2)) & mask;
                    uint element3 = (sdata >> (3 * 2)) & mask;
                    Unsafe.Add(ref d, 2) = element2;
                    Unsafe.Add(ref d, 3) = element3;

                    uint element4 = (sdata >> (4 * 2)) & mask;
                    uint element5 = (sdata >> (5 * 2)) & mask;
                    Unsafe.Add(ref d, 4) = element4;
                    Unsafe.Add(ref d, 5) = element5;

                    uint element6 = (sdata >> (6 * 2)) & mask;
                    uint element7 = (sdata >> (7 * 2)) & mask;
                    Unsafe.Add(ref d, 6) = element6;
                    Unsafe.Add(ref d, 7) = element7;

                    uint element8 = (sdata >> (8 * 2)) & mask;
                    uint element9 = (sdata >> (9 * 2)) & mask;
                    Unsafe.Add(ref d, 8) = element8;
                    Unsafe.Add(ref d, 9) = element9;

                    uint element10 = (sdata >> (10 * 2)) & mask;
                    uint element11 = (sdata >> (11 * 2)) & mask;
                    Unsafe.Add(ref d, 10) = element10;
                    Unsafe.Add(ref d, 11) = element11;

                    uint element12 = (sdata >> (12 * 2)) & mask;
                    uint element13 = (sdata >> (13 * 2)) & mask;
                    Unsafe.Add(ref d, 12) = element12;
                    Unsafe.Add(ref d, 13) = element13;

                    uint element14 = (sdata >> (14 * 2)) & mask;
                    uint element15 = (sdata >> (15 * 2)) & mask;
                    Unsafe.Add(ref d, 14) = element14;
                    Unsafe.Add(ref d, 15) = element15;

                    dstIndex += 16;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void BatchCopy3(uint iterations, ref ulong src, ref uint dst, ref int dstIndex)
        {
            const uint mask = ~(uint.MaxValue << 3);

            for (uint j = 0; j < iterations; j++)
            {
                ulong data = Unsafe.Add(ref src, (int)j);
                ref uint d = ref Unsafe.Add(ref dst, dstIndex);

                uint element0 = (uint)(data >> (0 * 3)) & mask;
                uint element1 = (uint)(data >> (1 * 3)) & mask;
                Unsafe.Add(ref d, 0) = element0;
                Unsafe.Add(ref d, 1) = element1;

                uint element2 = (uint)(data >> (2 * 3)) & mask;
                uint element3 = (uint)(data >> (3 * 3)) & mask;
                Unsafe.Add(ref d, 2) = element2;
                Unsafe.Add(ref d, 3) = element3;

                uint element4 = (uint)(data >> (4 * 3)) & mask;
                uint element5 = (uint)(data >> (5 * 3)) & mask;
                Unsafe.Add(ref d, 4) = element4;
                Unsafe.Add(ref d, 5) = element5;

                uint element6 = (uint)(data >> (6 * 3)) & mask;
                uint element7 = (uint)(data >> (7 * 3)) & mask;
                Unsafe.Add(ref d, 6) = element6;
                Unsafe.Add(ref d, 7) = element7;

                uint element8 = (uint)(data >> (8 * 3)) & mask;
                uint element9 = (uint)(data >> (9 * 3)) & mask;
                Unsafe.Add(ref d, 8) = element8;
                Unsafe.Add(ref d, 9) = element9;

                uint element10 = (uint)(data >> (10 * 3)) & mask;
                uint element11 = (uint)(data >> (11 * 3)) & mask;
                Unsafe.Add(ref d, 10) = element10;
                Unsafe.Add(ref d, 11) = element11;

                uint element12 = (uint)(data >> (12 * 3)) & mask;
                uint element13 = (uint)(data >> (13 * 3)) & mask;
                Unsafe.Add(ref d, 12) = element12;
                Unsafe.Add(ref d, 13) = element13;

                uint element14 = (uint)(data >> (14 * 3)) & mask;
                uint element15 = (uint)(data >> (15 * 3)) & mask;
                Unsafe.Add(ref d, 14) = element14;
                Unsafe.Add(ref d, 15) = element15;

                uint element16 = (uint)(data >> (16 * 3)) & mask;
                uint element17 = (uint)(data >> (17 * 3)) & mask;
                Unsafe.Add(ref d, 16) = element16;
                Unsafe.Add(ref d, 17) = element17;

                uint element18 = (uint)(data >> (18 * 3)) & mask;
                uint element19 = (uint)(data >> (19 * 3)) & mask;
                Unsafe.Add(ref d, 18) = element18;
                Unsafe.Add(ref d, 19) = element19;

                uint element20 = (uint)(data >> (20 * 3)) & mask;
                Unsafe.Add(ref d, 20) = element20;

                dstIndex += 21;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void BatchCopy4(uint iterations, ref ulong src, ref uint dst, ref int dstIndex)
        {
            const uint mask = ~(uint.MaxValue << 4);

            for (uint j = 0; j < iterations; j++)
            {
                ulong data = Unsafe.Add(ref src, (int)j);
                uint sdata0 = (uint)data;
                uint sdata1 = (uint)(data >> 32);
                ref uint d = ref Unsafe.Add(ref dst, dstIndex);

                {
                    uint element0 = (sdata0 >> (0 * 4)) & mask;
                    uint element1 = (sdata0 >> (1 * 4)) & mask;
                    Unsafe.Add(ref d, 0) = element0;
                    Unsafe.Add(ref d, 1) = element1;

                    uint element2 = (sdata0 >> (2 * 4)) & mask;
                    uint element3 = (sdata0 >> (3 * 4)) & mask;
                    Unsafe.Add(ref d, 2) = element2;
                    Unsafe.Add(ref d, 3) = element3;

                    uint element4 = (sdata0 >> (4 * 4)) & mask;
                    uint element5 = (sdata0 >> (5 * 4)) & mask;
                    Unsafe.Add(ref d, 4) = element4;
                    Unsafe.Add(ref d, 5) = element5;

                    uint element6 = (sdata0 >> (6 * 4)) & mask;
                    uint element7 = (sdata0 >> (7 * 4)) & mask;
                    Unsafe.Add(ref d, 6) = element6;
                    Unsafe.Add(ref d, 7) = element7;
                }

                {
                    uint element8 = (sdata1 >> (0 * 4)) & mask;
                    uint element9 = (sdata1 >> (1 * 4)) & mask;
                    Unsafe.Add(ref d, 8) = element8;
                    Unsafe.Add(ref d, 9) = element9;

                    uint element10 = (sdata1 >> (2 * 4)) & mask;
                    uint element11 = (sdata1 >> (3 * 4)) & mask;
                    Unsafe.Add(ref d, 10) = element10;
                    Unsafe.Add(ref d, 11) = element11;

                    uint element12 = (sdata1 >> (4 * 4)) & mask;
                    uint element13 = (sdata1 >> (5 * 4)) & mask;
                    Unsafe.Add(ref d, 12) = element12;
                    Unsafe.Add(ref d, 13) = element13;

                    uint element14 = (sdata1 >> (6 * 4)) & mask;
                    uint element15 = (sdata1 >> (7 * 4)) & mask;
                    Unsafe.Add(ref d, 14) = element14;
                    Unsafe.Add(ref d, 15) = element15;
                }

                dstIndex += 16;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void BatchCopy5(uint iterations, ref ulong src, ref uint dst, ref int dstIndex)
        {
            const uint mask = ~(uint.MaxValue << 5);

            for (uint j = 0; j < iterations; j++)
            {
                ulong data = Unsafe.Add(ref src, (int)j);
                ref uint d = ref Unsafe.Add(ref dst, dstIndex);

                uint element0 = (uint)(data >> (0 * 5)) & mask;
                uint element1 = (uint)(data >> (1 * 5)) & mask;
                Unsafe.Add(ref d, 0) = element0;
                Unsafe.Add(ref d, 1) = element1;
                uint element2 = (uint)(data >> (2 * 5)) & mask;
                uint element3 = (uint)(data >> (3 * 5)) & mask;
                Unsafe.Add(ref d, 2) = element2;
                Unsafe.Add(ref d, 3) = element3;
                uint element4 = (uint)(data >> (4 * 5)) & mask;
                uint element5 = (uint)(data >> (5 * 5)) & mask;
                Unsafe.Add(ref d, 4) = element4;
                Unsafe.Add(ref d, 5) = element5;
                uint element6 = (uint)(data >> (6 * 5)) & mask;
                uint element7 = (uint)(data >> (7 * 5)) & mask;
                Unsafe.Add(ref d, 6) = element6;
                Unsafe.Add(ref d, 7) = element7;
                uint element8 = (uint)(data >> (8 * 5)) & mask;
                uint element9 = (uint)(data >> (9 * 5)) & mask;
                Unsafe.Add(ref d, 8) = element8;
                Unsafe.Add(ref d, 9) = element9;
                uint element10 = (uint)(data >> (10 * 5)) & mask;
                uint element11 = (uint)(data >> (11 * 5)) & mask;
                Unsafe.Add(ref d, 10) = element10;
                Unsafe.Add(ref d, 11) = element11;

                dstIndex += 12;
            }
        }
    }
}
