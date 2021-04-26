using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MCServerSharp.World
{
    public struct BitArray32
    {
        private const int LongBits = 64;
        private uint _elementsPerLong;
        private uint _bitsPerLong;
        private uint _elementMask;

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
            _bitsPerLong = _elementsPerLong * BitsPerElement;

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

            startIndex += ElementOffset;

            int dstIndex = 0;
            uint dstLength = (uint)destination.Length;
            uint srcOffset = startIndex / _elementsPerLong;
            uint mask = _elementMask;

            ref ulong src = ref Store[srcOffset];
            ref uint dst = ref destination[0];

            uint startIndexRemainder = startIndex % _elementsPerLong;
            if (startIndexRemainder != 0)
            {
                ulong firstData = src;
                uint firstCount = Math.Min(
                     _elementsPerLong - startIndexRemainder,
                    Math.Min(dstLength, srcElementsLeft));

                for (; dstIndex < firstCount; dstIndex++)
                {
                    uint startOffset = ((uint)dstIndex + startIndexRemainder) * BitsPerElement;
                    uint element = (uint)(firstData >> (int)startOffset) & mask;
                    Unsafe.Add(ref dst, dstIndex) = element;
                }

                src = ref Unsafe.Add(ref src, 1);
                dstLength -= firstCount;
                srcElementsLeft -= firstCount;
            }

            uint iterations = Math.Min(dstLength, srcElementsLeft) / _elementsPerLong;
            int bitsPerLong = (int)(_elementsPerLong * BitsPerElement);
            int bitsPerElement = (int)BitsPerElement;

            //object bro = dic.GetOrAdd(bitsPerElement, (object)0);
            //ref int xd = ref Unsafe.Unbox<int>(bro);
            //System.Threading.Interlocked.Increment(ref xd);

            if (bitsPerElement == 4)
            {
                const uint mask4 = ~(uint.MaxValue << 4);
            
                for (uint j = 0; j < iterations; j++)
                {
                    ulong data = Unsafe.Add(ref src, (int)j);
                    uint sdata0 = (uint)data;
                    uint sdata1 = (uint)(data >> 32);
                    ref uint d = ref Unsafe.Add(ref dst, dstIndex);
            
                    uint element0 = (sdata0 >> (0 * 4)) & mask4;
                    uint element1 = (sdata0 >> (1 * 4)) & mask4;
                    Unsafe.Add(ref d, 0) = element0;
                    Unsafe.Add(ref d, 1) = element1;
                    uint element2 = (sdata0 >> (2 * 4)) & mask4;
                    uint element3 = (sdata0 >> (3 * 4)) & mask4;
                    Unsafe.Add(ref d, 2) = element2;
                    Unsafe.Add(ref d, 3) = element3;
                    uint element4 = (sdata0 >> (4 * 4)) & mask4;
                    uint element5 = (sdata0 >> (5 * 4)) & mask4;
                    Unsafe.Add(ref d, 4) = element4;
                    Unsafe.Add(ref d, 5) = element5;
                    uint element6 = (sdata0 >> (6 * 4)) & mask4;
                    uint element7 = (sdata0 >> (7 * 4)) & mask4;
                    Unsafe.Add(ref d, 6) = element6;
                    Unsafe.Add(ref d, 7) = element7;
            
                    uint element8 = (sdata1 >> (0 * 4)) & mask4;
                    uint element9 = (sdata1 >> (1 * 4)) & mask4;
                    Unsafe.Add(ref d, 8) = element8;
                    Unsafe.Add(ref d, 9) = element9;
                    uint element10 = (sdata1 >> (2 * 4)) & mask4;
                    uint element11 = (sdata1 >> (3 * 4)) & mask4;
                    Unsafe.Add(ref d, 10) = element10;
                    Unsafe.Add(ref d, 11) = element11;
                    uint element12 = (sdata1 >> (4 * 4)) & mask4;
                    uint element13 = (sdata1 >> (5 * 4)) & mask4;
                    Unsafe.Add(ref d, 12) = element12;
                    Unsafe.Add(ref d, 13) = element13;
                    uint element14 = (sdata1 >> (6 * 4)) & mask4;
                    uint element15 = (sdata1 >> (7 * 4)) & mask4;
                    Unsafe.Add(ref d, 14) = element14;
                    Unsafe.Add(ref d, 15) = element15;
            
                    dstIndex += 16;
                }
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

            dstLength -= _elementsPerLong * iterations;
            srcElementsLeft -= _elementsPerLong * iterations;

            // Try to copy the remaining elements that were not copied by batch.
            ulong lastData = Unsafe.Add(ref src, (int)iterations);
            uint lastCount = Math.Min(dstLength, srcElementsLeft);

            for (uint i = 0; i < lastCount; i++, dstIndex++)
            {
                uint startOffset = i * BitsPerElement;
                uint element = (uint)(lastData >> (int)startOffset) & mask;
                Unsafe.Add(ref dst, dstIndex) = element;
            }

            return dstIndex;
        }

        public static ConcurrentDictionary<int, object> dic = new();
    }
}
