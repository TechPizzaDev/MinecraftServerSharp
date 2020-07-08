using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.Collections
{
    public struct ByteStack<T> : IDisposable
        where T : struct
    {
        private byte[] _rentedBuffer;
        private bool _clearOnReturn;

        public int TopOfStack { get; private set; }

        public int ByteCapacity => _rentedBuffer.Length;
        public int Capacity => ByteCapacity / Unsafe.SizeOf<T>();

        public int ByteCount => ByteCapacity - TopOfStack;
        public int Count => ByteCount / Unsafe.SizeOf<T>();

        public ByteStack(int initialSize, bool clearOnReturn = true)
        {
            _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialSize * Unsafe.SizeOf<T>());
            _clearOnReturn = clearOnReturn;

            TopOfStack = _rentedBuffer.Length;
        }

        public void Dispose()
        {
            byte[] toReturn = _rentedBuffer;
            _rentedBuffer = null!;
            TopOfStack = 0;

            if (toReturn != null)
                ArrayPool<byte>.Shared.Return(toReturn, _clearOnReturn);
        }
        public void Push(in T item)
        {
            if (TopOfStack < Unsafe.SizeOf<T>())
                Enlarge();

            TopOfStack -= Unsafe.SizeOf<T>();
            MemoryMarshal.Write(_rentedBuffer.AsSpan(TopOfStack), ref Unsafe.AsRef(item));
        }

        public T Pop()
        {
            if (TopOfStack > _rentedBuffer.Length - Unsafe.SizeOf<T>())
                throw new InvalidOperationException();

            var item = MemoryMarshal.Read<T>(_rentedBuffer.AsSpan(TopOfStack));
            TopOfStack += Unsafe.SizeOf<T>();
            return item;
        }

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (TopOfStack > _rentedBuffer.Length - Unsafe.SizeOf<T>())
            {
                item = default;
                return false;
            }

            item = MemoryMarshal.Read<T>(_rentedBuffer.AsSpan(TopOfStack));
            TopOfStack += Unsafe.SizeOf<T>();
            return true;
        }

        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            if (TopOfStack > _rentedBuffer.Length - Unsafe.SizeOf<T>())
            {
                item = default;
                return false;
            }

            item = MemoryMarshal.Read<T>(_rentedBuffer.AsSpan(TopOfStack));
            return true;
        }

        private void Enlarge()
        {
            byte[] toReturn = _rentedBuffer;
            _rentedBuffer = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);

            Buffer.BlockCopy(
                toReturn,
                TopOfStack,
                _rentedBuffer,
                _rentedBuffer.Length - toReturn.Length + TopOfStack,
                toReturn.Length - TopOfStack);

            TopOfStack += _rentedBuffer.Length - toReturn.Length;

            ArrayPool<byte>.Shared.Return(toReturn, _clearOnReturn);
        }
    }
}
