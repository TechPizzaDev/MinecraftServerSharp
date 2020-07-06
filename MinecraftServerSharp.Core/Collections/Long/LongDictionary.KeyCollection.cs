// Copied from .NET Foundation (and Modified)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MinecraftServerSharp.Collections
{
    public partial class LongDictionary<TKey, TValue> where TKey : notnull
    {
        [DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly LongDictionary<TKey, TValue> _dictionary;

            public KeyCollection(LongDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dictionary);
            }

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (array.Length - index < _dictionary.Count)
                    throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

                int count = _dictionary._count;
                Entry[]? entries = _dictionary._entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries![i].Next >= -1)
                        array[index++] = entries[i].Key;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item)
            {
                throw CollectionExceptions.NotSupported_KeyCollectionSet();
            }

            void ICollection<TKey>.Clear()
            {
                throw CollectionExceptions.NotSupported_KeyCollectionSet();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return _dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw CollectionExceptions.NotSupported_KeyCollectionSet();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(_dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(_dictionary);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (array.Rank != 1)
                    throw CollectionExceptions.Argument_MultiDimArrayNotSupported(nameof(array));

                if (array.GetLowerBound(0) != 0)
                    throw CollectionExceptions.Argument_NonZeroLowerBound(nameof(array));

                if ((uint)index > (uint)array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (array.Length - index < _dictionary.Count)
                    throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    if (!(array is object[] objects))
                        throw CollectionExceptions.Argument_InvalidArrayType(nameof(array));

                    int count = _dictionary._count;
                    Entry[]? entries = _dictionary._entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries![i].Next >= -1)
                                objects[index++] = entries[i].Key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw CollectionExceptions.Argument_InvalidArrayType(nameof(array));
                    }
                }
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private readonly LongDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                [AllowNull, MaybeNull] private TKey _currentKey;

                internal Enumerator(LongDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentKey = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if (_version != _dictionary._version)
                        throw CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                    while ((uint)_index < (uint)_dictionary._count)
                    {
                        ref Entry entry = ref _dictionary._entries![_index++];
                        if (entry.Next >= -1)
                        {
                            _currentKey = entry.Key;
                            return true;
                        }
                    }

                    _index = _dictionary._count + 1;
                    _currentKey = default;
                    return false;
                }

                public TKey Current => _currentKey!;

                object? IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || (_index == _dictionary._count + 1))
                            throw CollectionExceptions.InvalidOperation_EnumerationCantHappen();

                        return _currentKey;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (_version != _dictionary._version)
                        throw CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                    _index = 0;
                    _currentKey = default;
                }
            }
        }
    }
}