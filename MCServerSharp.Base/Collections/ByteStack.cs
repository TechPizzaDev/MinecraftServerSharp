using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    public ref struct ByteStack<T>
        where T : unmanaged
    {
        private Span<byte> _buffer;
        private byte[]? _rentedBuffer;
        private bool _clearOnReturn;

        public int TopOfStack { get; private set; }

        public int ByteCapacity => _buffer.Length;
        public int Capacity => ByteCapacity / Unsafe.SizeOf<T>();

        public int ByteCount => ByteCapacity - TopOfStack;
        public int Count => ByteCount / Unsafe.SizeOf<T>();

        public bool IsEmpty => TopOfStack == ByteCapacity;

        public ByteStack(Span<T> initialBuffer, bool clearOnReturn = true)
        {
            _buffer = MemoryMarshal.AsBytes(initialBuffer);
            _rentedBuffer = null;
            _clearOnReturn = clearOnReturn;

            TopOfStack = _buffer.Length;
        }

        public void Dispose()
        {
            byte[]? toReturn = _rentedBuffer;
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
            MemoryMarshal.Write(_buffer[TopOfStack..], ref Unsafe.AsRef(item));
        }

        public T Pop()
        {
            if (TopOfStack > _buffer.Length - Unsafe.SizeOf<T>())
                throw new InvalidOperationException();

            var item = MemoryMarshal.Read<T>(_rentedBuffer.AsSpan(TopOfStack));
            TopOfStack += Unsafe.SizeOf<T>();
            return item;
        }

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (TopOfStack > _buffer.Length - Unsafe.SizeOf<T>())
            {
                item = default;
                return false;
            }

            item = MemoryMarshal.Read<T>(_buffer[TopOfStack..]);
            TopOfStack += Unsafe.SizeOf<T>();
            return true;
        }

        public bool TryPop()
        {
            if (TopOfStack > _buffer.Length - Unsafe.SizeOf<T>())
                return false;

            TopOfStack += Unsafe.SizeOf<T>();
            return true;
        }

        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            if (TopOfStack > _buffer.Length - Unsafe.SizeOf<T>())
            {
                item = default;
                return false;
            }

            item = MemoryMarshal.Read<T>(_buffer[TopOfStack..]);
            return true;
        }

        public ref T TryPeek()
        {
            if (TopOfStack > _buffer.Length - Unsafe.SizeOf<T>())
                return ref Unsafe.NullRef<T>();

            return ref MemoryMarshal.AsRef<T>(_buffer[TopOfStack..]);
        }

        private void Enlarge()
        {
            byte[]? toReturn = _rentedBuffer;
            _rentedBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);

            Span<byte> src = _buffer[TopOfStack..];
            Span<byte> dst = _rentedBuffer.AsSpan(_rentedBuffer.Length - _buffer.Length + TopOfStack);
            src.CopyTo(dst);

            TopOfStack += _rentedBuffer.Length - _buffer.Length;
            _buffer = _rentedBuffer;

            if (toReturn != null)
                ArrayPool<byte>.Shared.Return(toReturn, _clearOnReturn);
        }
    }
}
