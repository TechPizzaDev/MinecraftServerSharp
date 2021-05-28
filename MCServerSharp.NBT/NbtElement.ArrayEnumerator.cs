using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MCServerSharp.Utility;

namespace MCServerSharp.NBT
{
    public readonly partial struct NbtElement
    {
        /// <summary>
        /// An enumerable and enumerator for the contents of an NBT array.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ArrayEnumerator<T> : IEnumerable<T>, IEnumerator<T>
            where T : unmanaged
        {
            private readonly NbtElement _array;
            private readonly ReadOnlyMemory<byte> _arrayData;
            private int _currentIndex;

            public T Current
            {
                get
                {
                    if (_currentIndex < 0)
                        return default;

                    var slice = _arrayData[_currentIndex..].Span;

                    if (typeof(T) == typeof(int))
                    {
                        if (_array.Options.IsBigEndian)
                            return UnsafeR.As<int, T>(BinaryPrimitives.ReadInt32BigEndian(slice));
                        else
                            return UnsafeR.As<int, T>(BinaryPrimitives.ReadInt32LittleEndian(slice));
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        if (_array.Options.IsBigEndian)
                            return UnsafeR.As<long, T>(BinaryPrimitives.ReadInt64BigEndian(slice));
                        else
                            return UnsafeR.As<long, T>(BinaryPrimitives.ReadInt64LittleEndian(slice));
                    }
                    else
                    {
                        return MemoryMarshal.Read<T>(slice);
                    }
                }
            }

            object IEnumerator.Current => Current;

            internal ArrayEnumerator(NbtElement array)
            {
                _array = array;
                _arrayData = array._parent.GetArrayData(_array._index, out _);
                _currentIndex = -1;
            }

            public bool MoveNext()
            {
                if (_currentIndex < 0)
                    _currentIndex = 0;
                else
                    _currentIndex += Unsafe.SizeOf<T>();

                return _currentIndex != _arrayData.Length;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public void Dispose()
            {
            }

            /// <summary>
            /// Returns an enumerator that iterates through an array.
            /// </summary>
            /// <returns>
            /// An <see cref="ArrayEnumerator{T}"/> that can be used to iterate through the array.
            /// </returns>
            public ArrayEnumerator<T> GetEnumerator()
            {
                ArrayEnumerator<T> ator = this;
                ator.Reset();
                return ator;
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
