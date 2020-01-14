using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.Utility
{
    public unsafe class UnmanagedPointer<T> : IDisposable
        where T : unmanaged
    {
        private int _length;
        private T* _ptr;
        private object _allocMutex = new object();

        #region Properties + Indexer

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the length of the pointer in bytes.
        /// </summary>
        public int ByteLength
        {
            get
            {
                AssertNotDisposed();
                return GetBytes(_length);
            }
        }
        
        /// <summary>
        /// Gets or sets the length of the pointer in elements.
        /// </summary>
        public int Capacity
        {
            get
            {
                AssertNotDisposed();
                return _length;
            }
            set => ReAlloc(value);
        }

        public T* Ptr
        {
            get
            {
                AssertNotDisposed();
                if (_ptr == null)
                    throw new InvalidOperationException(
                        "There is no underlying memory allocated.");
                return _ptr;
            }
        }

        public IntPtr SafePtr => (IntPtr)Ptr;

        public Span<T> Span => new Span<T>(Ptr, _length);

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref Ptr[index];
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
        public UnmanagedPointer(int length, bool zeroFill = false)
        {
            ReAlloc(length, zeroFill);
        }

        /// <summary>
        /// Constructs the unmanaged pointer with a null pointer and zero length.
        /// </summary>
        public UnmanagedPointer() : this(0)
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

        public void Fill(byte value)
        {
            MemoryMarshal.AsBytes(Span).Fill(value);
        }

        #region Helpers

        /// <summary>
        /// Resizes the underlying memory block, 
        /// optionally zero-filling newly allocated memory.
        /// </summary>
        /// <param name="length">The new size in elements. Can be zero to free memory.</param>
        /// <param name="zeroFill"><see langword="true"/> to zero-fill the allocated memory.</param>
        public void ReAlloc(int length, bool zeroFill = false)
        {
            lock (_allocMutex)
            {
                AssertNotDisposed();
                if(length < 0)
                    throw new ArgumentOutOfRangeException(nameof(length));
                
                if (_length != length)
                {
                    int oldLength = _length;
                    _length = length;

                    if (length == 0)
                    {
                        FreePtr(oldLength);
                    }
                    else
                    {
                        ClearPressure(oldLength);
                        GC.AddMemoryPressure(ByteLength);

                        if (_ptr != null)
                            _ptr = (T*)Marshal.ReAllocHGlobal((IntPtr)_ptr, (IntPtr)ByteLength);
                        else
                            _ptr = (T*)Marshal.AllocHGlobal(ByteLength);

                        if (zeroFill && length > oldLength)
                            Span.Slice(oldLength, length - oldLength).Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Informs the runtime that memory has been released.
        /// </summary>
        /// <param name="oldLength"></param>
        private void ClearPressure(int oldLength)
        {
            if(oldLength != 0)
                GC.RemoveMemoryPressure(GetBytes(oldLength));
        }

        /// <summary>
        /// Frees pointer, informs the runtime about released memory, and sets length to zero.
        /// </summary>
        /// <param name="oldLength"></param>
        private void FreePtr(int oldLength)
        {
            if (_ptr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_ptr);
                _ptr = null;

                ClearPressure(oldLength);
                _length = 0;
            }
        }
        
        private int GetBytes(int elementCount)
        {
            return elementCount * sizeof(T);
        }

        #endregion

        #region IDisposable

        [DebuggerHidden]
        protected void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(UnmanagedPointer<T>));
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_allocMutex)
            {
                if (!IsDisposed)
                {
                    FreePtr(_length);
                    IsDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnmanagedPointer()
        {
            Dispose(false);
        }

        #endregion
    }
}
