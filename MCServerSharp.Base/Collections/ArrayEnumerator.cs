using System;
using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        public T Current
        {
            get
            {
                if ((uint)_index >= (uint)_array.Length)
                    throw new InvalidOperationException();
                return _array[_index];
            }
        }

        object? IEnumerator.Current => Current;

        public ArrayEnumerator(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _index = -1;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            if ((uint)index >= (uint)_array.Length)
            {
                _index = _array.Length;
                return false;
            }
            _index = index;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }

        public static implicit operator ArrayEnumerator<T>(T[] array)
        {
            return new ArrayEnumerator<T>(array);
        }
    }
}
