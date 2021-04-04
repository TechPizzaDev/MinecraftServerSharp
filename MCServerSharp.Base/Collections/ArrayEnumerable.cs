using System;
using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public struct ArrayEnumerable<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        public T Current => _array[_index];
        object? IEnumerator.Current => Current;

        public ArrayEnumerable(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _index = -1;
        }

        public bool MoveNext()
        {
            return ++_index < _array.Length;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }

        public ArrayEnumerable<T> GetEnumerator()
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

        public static implicit operator ArrayEnumerable<T>(T[] array)
        {
            return new ArrayEnumerable<T>(array);
        }
    }
}
