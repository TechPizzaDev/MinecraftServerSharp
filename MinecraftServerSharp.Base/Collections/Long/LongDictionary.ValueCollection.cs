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
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly LongDictionary<TKey, TValue> _dictionary;

            public ValueCollection(LongDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dictionary);
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if ((uint)index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (array.Length - index < _dictionary.Count)
                    throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

                int count = _dictionary._count;
                Entry[]? entries = _dictionary._entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries![i].Next >= -1)
                        array[index++] = entries[i].Value;
                }
            }

            public int Count => _dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item)
            {
                throw CollectionExceptions.NotSupported_ValueCollectionSet();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw CollectionExceptions.NotSupported_ValueCollectionSet();
            }

            void ICollection<TValue>.Clear()
            {
                throw CollectionExceptions.NotSupported_ValueCollectionSet();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return _dictionary.ContainsValue(item);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
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

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    if ((uint)index > (uint)array.Length)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    if (array.Length - index < _dictionary.Count)
                        throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

                    if (!(array is object[] objects))
                        throw CollectionExceptions.Argument_InvalidArrayType(nameof(array));

                    int count = _dictionary._count;
                    Entry[]? entries = _dictionary._entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries![i].Next >= -1)
                                objects[index++] = entries[i].Value!;
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

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly LongDictionary<TKey, TValue> _dictionary;
                private int _index;
                private readonly int _version;
                [AllowNull, MaybeNull] private TValue _currentValue;

                internal Enumerator(LongDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _version = dictionary._version;
                    _index = 0;
                    _currentValue = default;
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
                            _currentValue = entry.Value;
                            return true;
                        }
                    }
                    _index = _dictionary._count + 1;
                    _currentValue = default;
                    return false;
                }

                public TValue Current => _currentValue!;

                object? IEnumerator.Current
                {
                    get
                    {
                        if (_index == 0 || (_index == _dictionary._count + 1))
                            throw CollectionExceptions.InvalidOperation_EnumerationCantHappen();

                        return _currentValue;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (_version != _dictionary._version)
                        throw CollectionExceptions.InvalidOperation_EnumerationFailedVersion();

                    _index = 0;
                    _currentValue = default;
                }
            }
        }
    }
}