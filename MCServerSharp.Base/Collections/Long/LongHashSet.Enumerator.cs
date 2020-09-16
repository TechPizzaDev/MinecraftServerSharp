// Copied from .NET Foundation (and Modified)

using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public partial class LongHashSet<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly LongHashSet<T> _hashSet;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(LongHashSet<T> hashSet)
            {
                _hashSet = hashSet;
                _version = hashSet._version;
                _index = 0;
                _current = default!;
            }

            public bool MoveNext()
            {
                if (_version != _hashSet._version)
                    CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_hashSet._count)
                {
                    ref Entry entry = ref _hashSet._entries![_index++];
                    if (entry.Next >= -1)
                    {
                        _current = entry.Value;
                        return true;
                    }
                }

                _index = _hashSet._count + 1;
                _current = default!;
                return false;
            }

            public T Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _hashSet._count + 1))
                        CollectionExceptions.InvalidOperation_EnumerationCantHappen();

                    return _current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _hashSet._version)
                    CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                _index = 0;
                _current = default!;
            }
        }
    }
}
