// Copied from .NET Foundation (and Modified)

using System.Collections;
using System.Collections.Generic;

namespace MinecraftServerSharp.Collections
{
    public partial class LongDictionary<TKey, TValue> where TKey : notnull
    {
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly LongDictionary<TKey, TValue> _dictionary;
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;

            internal Enumerator(LongDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = dictionary._version;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_dictionary._count)
                {
                    ref Entry entry = ref _dictionary._entries![_index++];

                    if (entry.Next >= -1)
                    {
                        _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                        CollectionExceptions.InvalidOperation_EnumerationCantHappen();

                    return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _dictionary._version)
                    CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}