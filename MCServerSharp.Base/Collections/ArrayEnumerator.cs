using System;
using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public struct ArrayEnumerator<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        public T Current { get; private set; }
        object? IEnumerator.Current => Current;

        public ArrayEnumerator(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _index = 0;
            Current = default!;
        }

        public bool MoveNext()
        {
            if ((uint)_index >= (uint)_array.Length)
                return false;

            Current = _array[_index++];
            return true;
        }

        public void Reset()
        {
            _index = 0;
        }

        public void Dispose()
        {
        }

        public ArrayEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator ArrayEnumerator<T>(T[] array)
        {
            return new ArrayEnumerator<T>(array);
        }
    }
}
