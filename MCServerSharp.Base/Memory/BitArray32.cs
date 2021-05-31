using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp
{
    public readonly partial struct BitArray32
    {
        private const int LongBits = 64;
        private readonly uint _elementsPerLong;
        private readonly uint _elementMask;

        public ulong[] Store { get; }
        public uint ElementOffset { get; }

        public uint ElementCapacity { get; }
        public uint BitsPerElement { get; }

        public uint this[uint index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        public uint this[int index]
        {
            get => Get((uint)index);
            set => Set((uint)index, value);
        }

        public BitArray32(
            ulong[] store, uint storeOffset, uint elementCapacity, uint bitsPerElement)
        {
            if (bitsPerElement > 32u)
                throw new ArgumentOutOfRangeException(nameof(bitsPerElement));
            if (storeOffset >= store.Length)
                throw new ArgumentOutOfRangeException(nameof(storeOffset));

            Store = store ?? throw new ArgumentNullException(nameof(store));
            ElementOffset = storeOffset;
            ElementCapacity = elementCapacity;
            BitsPerElement = bitsPerElement;

            _elementsPerLong = LongBits / BitsPerElement;
            _elementMask = GetElementMask(bitsPerElement);
        }

        public static uint GetElementMask(uint bitsPerElement)
        {
            if (bitsPerElement == 32)
                return uint.MaxValue;
            else
                return ~(uint.MaxValue << (int)bitsPerElement);
        }

        public static int GetLongCount(int elementCapacity, int bitsPerElement)
        {
            int elementsPerPart = LongBits / bitsPerElement;
            int longCount = (elementCapacity + elementsPerPart - 1) / elementsPerPart;
            return longCount;
        }

        public static BitArray32 Allocate(
            int elementCapacity, int bitsPerElement, bool pinned = false)
        {
            int longCount = GetLongCount(elementCapacity, bitsPerElement);
            ulong[] array = GC.AllocateArray<ulong>(longCount, pinned);
            return new BitArray32(array, 0, (uint)elementCapacity, (uint)bitsPerElement);
        }

        public static BitArray32 AllocateUninitialized(
            int elementCapacity, int bitsPerElement, bool pinned = false)
        {
            int longCount = GetLongCount(elementCapacity, bitsPerElement);
            ulong[] array = GC.AllocateUninitializedArray<ulong>(longCount, pinned);
            return new BitArray32(array, 0, (uint)elementCapacity, (uint)bitsPerElement);
        }

        public void Set(uint elementIndex, uint value)
        {
            uint actualIndex = ElementOffset + elementIndex;
            uint startLong = actualIndex / _elementsPerLong;
            int bitOffset = (int)(actualIndex % _elementsPerLong * BitsPerElement);
            ref ulong data = ref Store[startLong];
            data &= ~((ulong)_elementMask << bitOffset);
            data |= (ulong)(value & _elementMask) << bitOffset;
        }

        public uint Get(uint elementIndex)
        {
            uint actualIndex = ElementOffset + elementIndex;
            uint startLong = actualIndex / _elementsPerLong;
            int bitOffset = (int)(actualIndex % _elementsPerLong * BitsPerElement);
            uint element = (uint)(Store[startLong] >> bitOffset) & _elementMask;
            return element;
        }

        public void Fill(uint value)
        {
            if (value == 0)
            {
                Store.AsSpan().Clear();
                return;
            }

            uint startIndex = ElementOffset;
            uint srcElementsLeft = ElementCapacity - startIndex;

            uint srcOffset = startIndex / _elementsPerLong;
            int bitsPerElement = (int)BitsPerElement;

            ref ulong src = ref Store[srcOffset];

            ulong mask = 0;
            for (int i = 0; i < _elementsPerLong; i++)
            {
                mask |= (ulong)value << (i * bitsPerElement);
            }

            uint startIndexRemainder = startIndex % _elementsPerLong;
            if (startIndexRemainder != 0)
            {
                //ulong firstData = src;
                //uint firstCount = Math.Min(
                //    _elementsPerLong - startIndex,
                //    srcElementsLeft);
                //
                //for (; srcIndex < firstCount; srcIndex++)
                //{
                //    uint startOffset = ((uint)srcIndex + startIndexRemainder) * BitsPerElement;
                //    uint element = (uint)(firstData >> (int)startOffset) & _elementMask;
                //    Unsafe.Add(ref src = element;
                //}
                //
                //src = ref Unsafe.Add(ref src, 1);
                //dstLength -= firstCount;
                //srcElementsLeft -= firstCount;
                throw new NotImplementedException();
            }

            uint iterations = srcElementsLeft / _elementsPerLong;
            int bitsPerLong = (int)(_elementsPerLong * BitsPerElement);

            for (uint j = 0; j < iterations; j++)
            {
                src = mask;
                src = ref Unsafe.Add(ref src, 1);
            }

            srcElementsLeft -= _elementsPerLong * iterations;

            // Try to copy the remaining elements that were not copied by batch.
            //ulong lastData = Unsafe.Add(ref src, (int)iterations);
            //
            //for (uint i = 0; i < srcElementsLeft; i++, dstIndex++)
            //{
            //    uint startOffset = i * BitsPerElement;
            //    uint element = (uint)(lastData >> (int)startOffset) & _elementMask;
            //    Unsafe.Add(ref dst, dstIndex) = element;
            //}

            if (srcElementsLeft != 0)
                throw new NotImplementedException();
        }

        public void Clear()
        {
            Fill(0);
        }

        public BitArray32 Slice(int elementOffset, int elementCount)
        {
            throw new NotImplementedException();
        }

        public int Get(uint startIndex, Span<uint> destination)
        {
            return Get(Store, ElementOffset, ElementCapacity, BitsPerElement, startIndex, destination);
        }

        //public static int Get(ReadOnlySpan<ulong> source, uint bitsPerElement, Span<uint>)

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe static int Get(
            ReadOnlySpan<ulong> source,
            uint elementOffset,
            uint elementCapacity,
            uint bitsPerElement,
            uint sourceIndex,
            Span<uint> destination)
        {
            uint srcElementsLeft = elementCapacity - sourceIndex;
            if (srcElementsLeft == 0)
                return 0;
            if (srcElementsLeft > elementCapacity)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            uint elementsPerLong = LongBits / bitsPerElement;

            nint dstIndex = 0;
            uint dstLength = (uint)destination.Length;
            uint toCopy = Math.Min(dstLength, srcElementsLeft);
            uint actualStartIndex = sourceIndex + elementOffset;
            uint srcOffset = actualStartIndex / elementsPerLong;
            uint mask = GetElementMask(bitsPerElement);

            ref ulong src = ref Unsafe.Add(ref MemoryMarshal.GetReference(source), (nint)srcOffset);
            ref uint dst = ref MemoryMarshal.GetReference(destination);

            uint startIndexRemainder = actualStartIndex % elementsPerLong;
            if (startIndexRemainder != 0)
            {
                ulong firstData = src;
                uint firstCount = Math.Min(toCopy, elementsPerLong - startIndexRemainder);

                for (; dstIndex < firstCount; dstIndex++)
                {
                    uint startOffset = ((uint)dstIndex + startIndexRemainder) * bitsPerElement;
                    uint element = (uint)(firstData >> (int)startOffset) & mask;
                    Unsafe.Add(ref dst, dstIndex) = element;
                }

                src = ref Unsafe.Add(ref src, 1);
                toCopy -= firstCount;
            }

            nint iterations = (nint)(toCopy / elementsPerLong);
            int bitsPerLong = (int)(elementsPerLong * bitsPerElement);

            if (bitsPerElement == 1)
            {
                BatchCopy1(iterations, ref src, ref dst, ref dstIndex);
            }
            else if (bitsPerElement == 2)
            {
                BatchCopy2(iterations, ref src, ref dst, ref dstIndex);
            }
            else if (bitsPerElement == 3)
            {
                BatchCopy3(iterations, ref src, ref dst, ref dstIndex);
            }
            else if (bitsPerElement == 4)
            {
                BatchCopy4(iterations, ref src, ref dst, ref dstIndex);
            }
            else if (bitsPerElement == 5)
            {
                BatchCopy5(iterations, ref src, ref dst, ref dstIndex);
            }
            else
            {
                int bpe = (int)bitsPerElement;
                for (nint j = 0; j < iterations; j++)
                {
                    ulong data = Unsafe.Add(ref src, j);
                    for (int i = 0; i < bitsPerLong; i += bpe, dstIndex++)
                    {
                        uint element = (uint)(data >> i) & mask;
                        Unsafe.Add(ref dst, dstIndex) = element;
                    }
                }
            }

            toCopy -= elementsPerLong * (uint)iterations;

            // Try to copy the remaining elements that were not copied by batch.
            ulong lastData = Unsafe.Add(ref src, iterations);

            for (uint i = 0; i < toCopy; i++, dstIndex++)
            {
                uint startOffset = i * bitsPerElement;
                uint element = (uint)(lastData >> (int)startOffset) & mask;
                Unsafe.Add(ref dst, dstIndex) = element;
            }

            return (int)dstIndex;
        }

        // TODO: optimize/specialize cases
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Set(uint startIndex, Span<uint> values)
        {
            if (values.IsEmpty)
                return;

            uint actualStartIndex = startIndex + ElementOffset;
            nint startLong = (nint)(actualStartIndex / _elementsPerLong);
            int bpe = (int)BitsPerElement;

            uint s = 0;
            uint startIndexRemainder = actualStartIndex % _elementsPerLong;
            if (startIndexRemainder != 0)
            {
                uint firstCount = _elementsPerLong - startIndexRemainder;
                for (uint j = 0; j < firstCount; j++)
                {
                    Set(startIndex + j, values[(int)j]);
                }

                startLong++;
                s += firstCount;
            }

            nint iterations = (nint)(((uint)values.Length - s) / _elementsPerLong);
            ref uint src = ref MemoryMarshal.GetReference(values);
            ref uint offSrc = ref Unsafe.Add(ref src, (nint)s);
            ref ulong dst = ref MemoryMarshal.GetArrayDataReference(Store);

            if (_elementsPerLong == 16)
            {
                BatchSet16(iterations, ref offSrc, ref dst, ref startLong);
                s += (uint)iterations * _elementsPerLong;
            }
            else if (_elementsPerLong == 64)
            {
                BatchSet64(iterations, ref offSrc, ref dst, ref startLong);
                s += (uint)iterations * _elementsPerLong;
            }
            else
            {
                for (int i = 0; i < iterations; i++)
                {
                    ulong packed = 0;

                    ref uint data = ref Unsafe.Add(ref src, (int)s);
                    for (int j = 0; j < _elementsPerLong; j++)
                    {
                        packed |= (ulong)Unsafe.Add(ref data, (nint)j) << (j * bpe);
                    }

                    Unsafe.Add(ref dst, startLong++) = packed;
                    s += _elementsPerLong;
                }
            }

            for (; s < values.Length; s++)
            {
                Set(startIndex + s, values[(int)s]);
            }
        }

        public static void BatchSet64(nint iterations, ref uint src, ref ulong dst, ref nint dstIndex)
        {
            for (nint i = 0; i < iterations; i++)
            {
                ref uint data = ref Unsafe.Add(ref src, i * 64);

                uint low = 0;
                uint high = 0;

                low |= Unsafe.Add(ref data, 0) << 0;
                low |= Unsafe.Add(ref data, 1) << 1;
                low |= Unsafe.Add(ref data, 2) << 2;
                low |= Unsafe.Add(ref data, 3) << 3;
                low |= Unsafe.Add(ref data, 4) << 4;
                low |= Unsafe.Add(ref data, 5) << 5;
                low |= Unsafe.Add(ref data, 6) << 6;
                low |= Unsafe.Add(ref data, 7) << 7;
                low |= Unsafe.Add(ref data, 8) << 8;
                low |= Unsafe.Add(ref data, 9) << 9;
                low |= Unsafe.Add(ref data, 10) << 10;
                low |= Unsafe.Add(ref data, 11) << 11;
                low |= Unsafe.Add(ref data, 12) << 12;
                low |= Unsafe.Add(ref data, 13) << 13;
                low |= Unsafe.Add(ref data, 14) << 14;
                low |= Unsafe.Add(ref data, 15) << 15;

                low |= Unsafe.Add(ref data, 16) << 16;
                low |= Unsafe.Add(ref data, 17) << 17;
                low |= Unsafe.Add(ref data, 18) << 18;
                low |= Unsafe.Add(ref data, 19) << 19;
                low |= Unsafe.Add(ref data, 20) << 20;
                low |= Unsafe.Add(ref data, 21) << 21;
                low |= Unsafe.Add(ref data, 22) << 22;
                low |= Unsafe.Add(ref data, 23) << 23;
                low |= Unsafe.Add(ref data, 24) << 24;
                low |= Unsafe.Add(ref data, 25) << 25;
                low |= Unsafe.Add(ref data, 26) << 26;
                low |= Unsafe.Add(ref data, 27) << 27;
                low |= Unsafe.Add(ref data, 28) << 28;
                low |= Unsafe.Add(ref data, 29) << 29;
                low |= Unsafe.Add(ref data, 30) << 30;
                low |= Unsafe.Add(ref data, 31) << 31;

                high |= Unsafe.Add(ref data, 32) << 0;
                high |= Unsafe.Add(ref data, 33) << 1;
                high |= Unsafe.Add(ref data, 34) << 2;
                high |= Unsafe.Add(ref data, 35) << 3;
                high |= Unsafe.Add(ref data, 36) << 4;
                high |= Unsafe.Add(ref data, 37) << 5;
                high |= Unsafe.Add(ref data, 38) << 6;
                high |= Unsafe.Add(ref data, 39) << 7;
                high |= Unsafe.Add(ref data, 40) << 8;
                high |= Unsafe.Add(ref data, 41) << 9;
                high |= Unsafe.Add(ref data, 42) << 10;
                high |= Unsafe.Add(ref data, 43) << 11;
                high |= Unsafe.Add(ref data, 44) << 12;
                high |= Unsafe.Add(ref data, 45) << 13;
                high |= Unsafe.Add(ref data, 46) << 14;
                high |= Unsafe.Add(ref data, 47) << 15;

                high |= Unsafe.Add(ref data, 48) << 16;
                high |= Unsafe.Add(ref data, 49) << 17;
                high |= Unsafe.Add(ref data, 50) << 18;
                high |= Unsafe.Add(ref data, 51) << 19;
                high |= Unsafe.Add(ref data, 52) << 20;
                high |= Unsafe.Add(ref data, 53) << 21;
                high |= Unsafe.Add(ref data, 54) << 22;
                high |= Unsafe.Add(ref data, 55) << 23;
                high |= Unsafe.Add(ref data, 56) << 24;
                high |= Unsafe.Add(ref data, 57) << 25;
                high |= Unsafe.Add(ref data, 58) << 26;
                high |= Unsafe.Add(ref data, 59) << 27;
                high |= Unsafe.Add(ref data, 60) << 28;
                high |= Unsafe.Add(ref data, 61) << 29;
                high |= Unsafe.Add(ref data, 62) << 30;
                high |= Unsafe.Add(ref data, 63) << 31;

                Unsafe.Add(ref dst, dstIndex++) = low | ((ulong)high << 32);
            }
        }

        public static void BatchSet16(nint iterations, ref uint src, ref ulong dst, ref nint dstIndex)
        {
            for (nint i = 0; i < iterations; i++)
            {
                ref uint data = ref Unsafe.Add(ref src, i * 16);

                uint low = 0;
                uint high = 0;

                low |= Unsafe.Add(ref data, 0) << (0 * 4);
                low |= Unsafe.Add(ref data, 1) << (1 * 4);
                low |= Unsafe.Add(ref data, 2) << (2 * 4);
                low |= Unsafe.Add(ref data, 3) << (3 * 4);
                low |= Unsafe.Add(ref data, 4) << (4 * 4);
                low |= Unsafe.Add(ref data, 5) << (5 * 4);
                low |= Unsafe.Add(ref data, 6) << (6 * 4);
                low |= Unsafe.Add(ref data, 7) << (7 * 4);

                high |= Unsafe.Add(ref data, 8) << (0 * 4);
                high |= Unsafe.Add(ref data, 9) << (1 * 4);
                high |= Unsafe.Add(ref data, 10) << (2 * 4);
                high |= Unsafe.Add(ref data, 11) << (3 * 4);
                high |= Unsafe.Add(ref data, 12) << (4 * 4);
                high |= Unsafe.Add(ref data, 13) << (5 * 4);
                high |= Unsafe.Add(ref data, 14) << (6 * 4);
                high |= Unsafe.Add(ref data, 15) << (7 * 4);

                Unsafe.Add(ref dst, dstIndex++) = low | ((ulong)high << 32);
            }
        }
    }
}
