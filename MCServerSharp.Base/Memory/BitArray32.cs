using System;
using System.Runtime.CompilerServices;

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

            int dstIndex = 0;
            uint dstLength = (uint)destination.Length;
            uint toCopy = Math.Min(dstLength, srcElementsLeft);
            uint actualStartIndex = sourceIndex + elementOffset;
            uint srcOffset = actualStartIndex / elementsPerLong;
            uint mask = GetElementMask(bitsPerElement);

            ref ulong src = ref Unsafe.AsRef(source[(int)srcOffset]);
            ref uint dst = ref destination[0];

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

            uint iterations = toCopy / elementsPerLong;
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
                for (uint j = 0; j < iterations; j++)
                {
                    ulong data = Unsafe.Add(ref src, (int)j);
                    for (int i = 0; i < bitsPerLong; i += bpe, dstIndex++)
                    {
                        uint element = (uint)(data >> i) & mask;
                        Unsafe.Add(ref dst, dstIndex) = element;
                    }
                }
            }

            toCopy -= elementsPerLong * iterations;

            // Try to copy the remaining elements that were not copied by batch.
            ulong lastData = Unsafe.Add(ref src, (int)iterations);

            for (uint i = 0; i < toCopy; i++, dstIndex++)
            {
                uint startOffset = i * bitsPerElement;
                uint element = (uint)(lastData >> (int)startOffset) & mask;
                Unsafe.Add(ref dst, dstIndex) = element;
            }

            return dstIndex;
        }

        // TODO: optimize/specialize cases
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Set(uint startIndex, Span<uint> values)
        {
            if (values.IsEmpty)
                return;

            uint actualStartIndex = startIndex + ElementOffset;
            uint startLong = actualStartIndex / _elementsPerLong;
            int bpe = (int)BitsPerElement;

            uint i = 0;
            uint startIndexRemainder = actualStartIndex % _elementsPerLong;
            if (startIndexRemainder != 0)
            {
                uint firstCount = _elementsPerLong - startIndexRemainder;
                for (uint j = 0; j < firstCount; j++)
                {
                    Set(startIndex + j, values[(int)j]);
                }

                startLong++;
                i += firstCount;
            }

            while (values.Length - i >= _elementsPerLong)
            {
                ulong packed = 0;

                ref uint src = ref values[(int)i];
                for (int j = 0; j < _elementsPerLong; j++)
                {
                    packed |= (ulong)Unsafe.Add(ref src, j) << (j * bpe);
                }

                Store[(int)(startLong++)] = packed;

                i += _elementsPerLong;
            }

            for (; i < values.Length; i++)
            {
                Set(startIndex + i, values[(int)i]);
            }
        }
    }
}
