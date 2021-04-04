using System;
using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Memory
{
    public struct MemoryEnumerable<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly ReadOnlyMemory<T> _memory;
        private int _index;

        public T Current => _memory.Span[_index];
        object? IEnumerator.Current => Current;

        public MemoryEnumerable(ReadOnlyMemory<T> array)
        {
            _memory = array;
            _index = -1;
        }

        public bool MoveNext()
        {
            return ++_index < _memory.Length;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }

        public MemoryEnumerable<T> GetEnumerator()
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

        public static implicit operator MemoryEnumerable<T>(ReadOnlyMemory<T> array)
        {
            return new MemoryEnumerable<T>(array);
        }
    }
}
