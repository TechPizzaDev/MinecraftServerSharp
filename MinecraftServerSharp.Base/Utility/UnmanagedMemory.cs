using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.Utility
{
    public unsafe class UnmanagedMemory<T> : IMemory<T>, IDisposable
        where T : unmanaged
    {
        private int _length;

        #region Properties + Indexer

        public bool IsDisposed { get; private set; }

        public IntPtr Pointer { get; private set; }

        public int ElementSize => sizeof(T);

        public Span<T> Span => new Span<T>((void*)Pointer, _length);
        Span<byte> IMemory.Span => new Span<byte>((void*)Pointer, GetByteCount(_length));

        ReadOnlySpan<T> IReadOnlyMemory<T>.Span => new ReadOnlySpan<T>((void*)Pointer, _length);
        ReadOnlySpan<byte> IReadOnlyMemory.Span => new ReadOnlySpan<byte>((void*)Pointer, GetByteCount(_length));

        /// <summary>
        /// Gets the length of the allocated memory in bytes.
        /// </summary>
        public int ByteLength
        {
            get
            {
                AssertNotDisposed();
                return GetByteCount(_length);
            }
        }

        /// <summary>
        /// Gets or sets the length of the allocated memory in elements.
        /// </summary>
        public int Length

        {
            get
            {
                AssertNotDisposed();
                return _length;
            }
            set => ReAllocate(value);
        }

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var ptr = (T*)Pointer;
                return ref ptr[index];
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the unmanaged pointer with a specified length,
        /// optionally zero-filling the allocated memory.
        /// </summary>
        /// <param name="length">The size in elements.</param>
        /// <param name="zeroFill">
        /// <see langword="true"/> to zero-fill the allocated memory.</param>
        public UnmanagedMemory(int length, bool zeroFill = false)
        {
            ReAllocate(length, zeroFill);
        }

        /// <summary>
        /// Constructs the unmanaged pointer with a null pointer and zero length.
        /// </summary>
        public UnmanagedMemory() : this(0)
        {
        }

        #endregion

        public void Clear()
        {
            Span.Clear();
        }

        public void Fill(T value)
        {
            Span.Fill(value);
        }

        #region Helpers

        /// <summary>
        /// Resizes the underlying memory, 
        /// optionally zero-filling newly allocated memory.
        /// </summary>
        /// <param name="length">The new size in elements. Can be zero to free memory.</param>
        /// <param name="zeroFill"><see langword="true"/> to zero-fill the allocated memory.</param>
        public void ReAllocate(int length, bool zeroFill = false)
        {
            AssertNotDisposed();
            ArgumentGuard.AssertAtLeastZero(length, nameof(length));

            if (_length == length)
                return;

            int oldLength = _length;
            _length = length;
            if (_length == 0)
            {
                Free(oldLength);
            }
            else
            {
                Pointer = Pointer == default
                    ? Marshal.AllocHGlobal(ByteLength)
                    : Marshal.ReAllocHGlobal(Pointer, (IntPtr)ByteLength);

                int lengthDiff = _length - oldLength;
                int lengthByteDiff = GetByteCount(lengthDiff);
                if (lengthDiff > 0)
                {
                    GC.AddMemoryPressure(lengthByteDiff);

                    if (zeroFill)
                        Span.Slice(oldLength, lengthDiff).Clear();
                }
                else
                {
                    GC.RemoveMemoryPressure(-lengthByteDiff);
                }
            }
        }

        private void Free(int length)
        {
            if (Pointer == default)
                return;

            Marshal.FreeHGlobal(Pointer);
            Pointer = default;
            _length = 0;

            GC.RemoveMemoryPressure(GetByteCount(length));
        }

        private static int GetByteCount(int elementCount)
        {
            return elementCount * sizeof(T);
        }

        #endregion

        #region IDisposable

        [DebuggerHidden]
        protected void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(UnmanagedMemory<T>));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Free(_length);
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnmanagedMemory()
        {
            Dispose(false);
        }

        #endregion
    }
}
