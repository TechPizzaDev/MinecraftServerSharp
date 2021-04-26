using System;
using System.Runtime.CompilerServices;

namespace MCServerSharp.World
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

            if (BitsPerElement == 32)
                _elementMask = uint.MaxValue;
            else
                _elementMask = ~(uint.MaxValue << (int)BitsPerElement);
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
            uint startLong = (ElementOffset + elementIndex) / _elementsPerLong;
            int bitOffset = (int)(elementIndex % _elementsPerLong * BitsPerElement);
            ref ulong data = ref Store[startLong];
            data &= ~((ulong)_elementMask << bitOffset);
            data |= (ulong)(value & _elementMask) << bitOffset;
        }

        public uint Get(uint elementIndex)
        {
            uint startLong = (ElementOffset + elementIndex) / _elementsPerLong;
            ulong data = Store[startLong];
            int bitOffset = (int)(elementIndex % _elementsPerLong * BitsPerElement);
            uint element = (uint)(data >> bitOffset) & _elementMask;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe int Get(uint startIndex, Span<uint> destination)
        {
            uint srcElementsLeft = ElementCapacity - startIndex;
            if (srcElementsLeft > ElementCapacity)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int dstIndex = 0;
            uint dstLength = (uint)destination.Length;
            uint toCopy = Math.Min(dstLength, srcElementsLeft);
            uint actualStartIndex = startIndex + ElementOffset;
            uint srcOffset = actualStartIndex / _elementsPerLong;
            uint mask = _elementMask;

            ref ulong src = ref Store[srcOffset];
            ref uint dst = ref destination[0];

            uint startIndexRemainder = actualStartIndex % _elementsPerLong;
            if (startIndexRemainder != 0)
            {
                ulong firstData = src;
                uint firstCount = Math.Min(toCopy, _elementsPerLong - startIndexRemainder);

                for (; dstIndex < firstCount; dstIndex++)
                {
                    uint startOffset = ((uint)dstIndex + startIndexRemainder) * BitsPerElement;
                    uint element = (uint)(firstData >> (int)startOffset) & mask;
                    Unsafe.Add(ref dst, dstIndex) = element;
                }

                src = ref Unsafe.Add(ref src, 1);
                toCopy -= firstCount;
            }

            uint iterations = toCopy / _elementsPerLong;
            int bitsPerLong = (int)(_elementsPerLong * BitsPerElement);
            int bitsPerElement = (int)BitsPerElement;

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
                for (uint j = 0; j < iterations; j++)
                {
                    ulong data = Unsafe.Add(ref src, (int)j);
                    for (int i = 0; i < bitsPerLong; i += bitsPerElement, dstIndex++)
                    {
                        uint element = (uint)(data >> i) & mask;
                        Unsafe.Add(ref dst, dstIndex) = element;
                    }
                }
            }

            toCopy -= _elementsPerLong * iterations;

            // Try to copy the remaining elements that were not copied by batch.
            ulong lastData = Unsafe.Add(ref src, (int)iterations);
            
            for (uint i = 0; i < toCopy; i++, dstIndex++)
            {
                uint startOffset = i * BitsPerElement;
                uint element = (uint)(lastData >> (int)startOffset) & mask;
                Unsafe.Add(ref dst, dstIndex) = element;
            }

            return dstIndex;
        }
    }
}
