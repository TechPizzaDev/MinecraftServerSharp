using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
            private readonly int _targetEndIndex;
            private int _currentIndex;

            public T Current
            {
                get
                {
                    if (_currentIndex < 0)
                        return default;

                    throw new NotImplementedException();
                }
            }

            object IEnumerator.Current => Current;

            internal ArrayEnumerator(NbtElement array)
            {
                _array = array;
                _targetEndIndex = array._parent.GetEndIndex(_array._index, false);
                _currentIndex = -1;
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {

            }

            public void Dispose()
            {
                _currentIndex = _targetEndIndex;
            }

            /// <summary>
            /// Returns an enumerator that iterates through an array.
            /// </summary>
            /// <returns>
            /// A <see cref="ArrayEnumerator"/> that can be used to iterate through the array.
            /// </returns>
            public ArrayEnumerator<T> GetEnumerator()
            {
                ArrayEnumerator<T> ator = this;
                ator._currentIndex = -1;
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
