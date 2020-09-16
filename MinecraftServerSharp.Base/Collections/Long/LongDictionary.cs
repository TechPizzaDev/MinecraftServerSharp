// Copied from .NET Foundation (and Modified)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MCServerSharp.Utility;

namespace MCServerSharp.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public partial class LongDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private const int StartOfFreeList = -3;

        private int[]? _buckets;
        private Entry[]? _entries;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
        private KeyCollection? _keys;
        private ValueCollection? _values;

        public int Count => _count - _freeCount;

        /// <summary>
        /// Gets the <see cref="ILongEqualityComparer{T}"/> object that is 
        /// used to determine equality for the values in the set.
        /// </summary>
        public ILongEqualityComparer<TKey> Comparer { get; private set; }

        public KeyCollection Keys => _keys ??= new KeyCollection(this);
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        public ValueCollection Values => _values ??= new ValueCollection(this);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public TValue this[TKey key]
        {
            get
            {
                ref TValue value = ref FindValue(key);
                if (!UnsafeR.IsNullRef(ref value))
                    return value;
                throw new KeyNotFoundException();
            }
            set
            {
                bool modified = TryInsert(key, value, LongInsertionBehavior.OverwriteExisting);
                Debug.Assert(modified);
            }
        }

        #region

        public LongDictionary() : this(0, null)
        {
        }

        public LongDictionary(int capacity) : this(capacity, null)
        {
        }

        public LongDictionary(ILongEqualityComparer<TKey>? comparer) : this(0, comparer)
        {
        }

        public LongDictionary(int capacity, ILongEqualityComparer<TKey>? comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity > 0)
                Initialize(capacity);

            if (comparer == null && typeof(TKey) == typeof(string))
            {
                // To start, move off default comparer for string which is randomised
                comparer = (ILongEqualityComparer<TKey>)NonRandomLongStringComparer.Default;
            }
            Comparer = comparer ?? LongEqualityComparer<TKey>.Default;
        }

        public LongDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null)
        {
        }

        public LongDictionary(IDictionary<TKey, TValue> dictionary, ILongEqualityComparer<TKey>? comparer) :
            this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary.GetType() == typeof(LongDictionary<TKey, TValue>))
            {
                var d = (LongDictionary<TKey, TValue>)dictionary;
                int count = d._count;
                Entry[]? entries = d._entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries![i].Next >= -1)
                        Add(entries[i].Key, entries[i].Value);
                }
            }
            else
            {
                foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                    Add(pair.Key, pair.Value);
            }
        }

        public LongDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null)
        {
        }

        public LongDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> collection, ILongEqualityComparer<TKey>? comparer) :
            this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            int? count = CollectionHelper.TryGetCount(collection);
            if (count.HasValue)
                Initialize(count.Value);

            foreach (KeyValuePair<TKey, TValue> pair in collection)
                Add(pair.Key, pair.Value);
        }

        #endregion

        public void Add(TKey key, TValue value)
        {
            bool modified = TryInsert(key, value, LongInsertionBehavior.ThrowOnExisting);

            // If there was an existing key and the Add failed, an exception will already have been thrown.
            Debug.Assert(modified);
        }

        public void Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ref TValue value = ref FindValue(keyValuePair.Key);
            if (!UnsafeR.IsNullRef(ref value) &&
                LongEqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value))
            {
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ref TValue value = ref FindValue(keyValuePair.Key);
            if (!UnsafeR.IsNullRef(ref value) &&
                LongEqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value))
            {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            int count = _count;
            if (count > 0)
            {
                Debug.Assert(_buckets != null, "_buckets should be non-null");
                Debug.Assert(_entries != null, "_entries should be non-null");

                Array.Clear(_buckets, 0, _buckets.Length);

                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return !UnsafeR.IsNullRef(ref FindValue(key));
        }

        public bool ContainsValue(TValue value)
        {
            Entry[]? entries = _entries;
            if (value == null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && entries[i].Value == null)
                        return true;
                }
            }
            else if (typeof(TValue).IsValueType)
            {
                var defaultComparer = LongEqualityComparer<TValue>.Default;
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && defaultComparer.Equals(entries[i].Value, value))
                        return true;
                }
            }
            else
            {
                var defaultComparer = LongEqualityComparer<TValue>.Default;
                for (int i = 0; i < _count; i++)
                {
                    if (entries![i].Next >= -1 && defaultComparer.Equals(entries[i].Value, value))
                        return true;
                }
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if ((uint)index > (uint)array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array.Length - index < Count)
                throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

            int count = _count;
            Entry[]? entries = _entries;
            for (int i = 0; i < count; i++)
            {
                if (entries![i].Next >= -1)
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].Key, entries[i].Value);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        private ref TValue FindValue(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            ref Entry entry = ref UnsafeR.NullRef<Entry>();
            if (_buckets != null)
            {
                Debug.Assert(_entries != null, "expected entries to be != null");
                ILongEqualityComparer<TKey> comparer = Comparer;

                long hashCode = comparer.GetLongHashCode(key);
                int i = GetBucket(hashCode);
                Entry[]? entries = _entries;
                uint collisionCount = 0;

                // Value in _buckets is 1-based; subtract 1 from i.
                i--; // We do it here so it fuses with the following conditional.

                do
                {
                    if ((uint)i >= (uint)entries.Length)
                        return ref UnsafeR.NullRef<TValue>();

                    entry = ref entries[i];
                    if (entry.HashCode == hashCode && comparer.Equals(entry.Key, key))
                        return ref entry.Value;

                    i = entry.Next;

                    collisionCount++;
                } while (collisionCount <= (uint)entries.Length);

                // The chain of entries forms a loop; which means a concurrent update has happened.
                // Break out of the loop and throw, rather than looping forever.
                throw CollectionExceptions.InvalidOperation_ConcurrentOperations();
            }

            return ref UnsafeR.NullRef<TValue>();
        }

        private int Initialize(int capacity)
        {
            int size = LongHashHelpers.GetPrime(capacity);
            var buckets = new int[size];
            var entries = new Entry[size];

            // Assign member variables after both arrays allocated to 
            // guard against corruption from OOM if second fails
            _freeList = -1;

            _buckets = buckets;
            _entries = entries;

            return size;
        }

        private bool TryInsert(TKey key, TValue value, LongInsertionBehavior behavior)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_buckets == null)
                Initialize(0);
            Debug.Assert(_buckets != null);

            Entry[]? entries = _entries;
            Debug.Assert(entries != null, "expected entries to be non-null");

            ILongEqualityComparer<TKey> comparer = Comparer;
            long hashCode = comparer.GetHashCode(key);

            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket - 1; // Value in _buckets is 1-based

            while ((uint)i >= (uint)entries.Length)
            {
                if (entries[i].HashCode == hashCode && comparer.Equals(entries[i].Key, key))
                {
                    if (behavior == LongInsertionBehavior.OverwriteExisting)
                    {
                        entries[i].Value = value;
                        return true;
                    }

                    if (behavior == LongInsertionBehavior.ThrowOnExisting)
                        throw CollectionExceptions.Argument_DuplicateKey(key?.ToString(), nameof(key));

                    return false;
                }

                i = entries[i].Next;

                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw CollectionExceptions.InvalidOperation_ConcurrentOperations();
                }
            }

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                Debug.Assert(
                    (StartOfFreeList - entries[_freeList].Next) >= -1,
                    "shouldn't overflow because `next` cannot underflow");

                _freeList = StartOfFreeList - entries[_freeList].Next;
                _freeCount--;
            }
            else
            {
                int count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries;
            }

            ref Entry entry = ref entries![index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Key = key;
            entry.Value = value; // Value in _buckets is 1-based
            bucket = index + 1;
            _version++;

            if (!typeof(TKey).IsValueType && // Value types never rehash
                collisionCount > LongHashHelpers.HashCollisionThreshold &&
                comparer is NonRandomLongStringComparer)
            {
                // If we hit the collision threshold we'll need to 
                // switch to the comparer which is using randomized string hashing
                // i.e. LongEqualityComparer<string>.Default.
                Comparer = LongEqualityComparer<TKey>.Default;
                Resize(entries.Length, forceNewHashCodes: true);
            }

            return true;
        }

        private void Resize()
        {
            Resize(LongHashHelpers.ExpandPrime(_count), forceNewHashCodes: false);
        }

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            // Value types never rehash
            Debug.Assert(!forceNewHashCodes || !typeof(TKey).IsValueType);
            Debug.Assert(_entries != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Length);

            var entries = new Entry[newSize];

            int count = _count;
            Array.Copy(_entries, entries, count);

            if (!typeof(TKey).IsValueType && forceNewHashCodes)
            {
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].Next >= -1)
                        entries[i].HashCode = Comparer.GetLongHashCode(entries[i].Key);
                }
            }

            // Assign member variables after both arrays allocated to 
            // guard against corruption from OOM if second fails
            _buckets = new int[newSize];

            for (int i = 0; i < count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    ref int bucket = ref GetBucket(entries[i].HashCode);
                    entries[i].Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entries = entries;
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_buckets != null)
            {
                Debug.Assert(_entries != null, "entries should be non-null");
                uint collisionCount = 0;
                long hashCode = Comparer.GetLongHashCode(key);
                ref int bucket = ref GetBucket(hashCode);
                Entry[]? entries = _entries;
                int last = -1;
                int i = bucket - 1; // Value in buckets is 1-based
                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];

                    if (entry.HashCode == hashCode && Comparer.Equals(entry.Key, key))
                    {
                        if (last < 0)
                            bucket = entry.Next + 1; // Value in buckets is 1-based
                        else
                            entries[last].Next = entry.Next;

                        value = entry.Value;

                        Debug.Assert(
                            (StartOfFreeList - _freeList) < 0,
                            "shouldn't underflow because max hashtable length is " +
                            "MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");

                        entry.Next = StartOfFreeList - _freeList;

                        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
                            entry.Key = default!;

                        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
                            entry.Value = default!;

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.Next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        throw CollectionExceptions.InvalidOperation_ConcurrentOperations();
                    }
                }
            }

            value = default;
            return false;
        }

        public bool Remove(TKey key)
        {
            return Remove(key, out _);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ref TValue valRef = ref FindValue(key);
            if (!UnsafeR.IsNullRef(ref valRef))
            {
                value = valRef;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            return TryInsert(key, value, LongInsertionBehavior.None);
        }

        public bool IsReadOnly => false;

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw CollectionExceptions.Argument_MultiDimArrayNotSupported(nameof(array));

            if (array.GetLowerBound(0) != 0)
                throw CollectionExceptions.Argument_NonZeroLowerBound(nameof(array));

            if ((uint)index > (uint)array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array.Length - index < Count)
                throw CollectionExceptions.Argument_ArrayPlusOffTooSmall();

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                CopyTo(pairs, index);
            }
            else
            {
                if (!(array is object[] objects))
                    throw CollectionExceptions.Argument_InvalidArrayType(nameof(array));

                try
                {
                    int count = _count;
                    Entry[]? entries = _entries;
                    for (int i = 0; i < count; i++)
                    {
                        if (entries![i].Next >= -1)
                            objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].Key, entries[i].Value);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw CollectionExceptions.Argument_InvalidArrayType(nameof(array));
                }
            }
        }

        /// <summary>
        /// Ensures that the dictionary can hold up to 'capacity' entries
        /// without any further expansion of its backing storage
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if (currentCapacity >= capacity)
                return currentCapacity;

            _version++;

            if (_buckets == null)
                return Initialize(capacity);

            int newSize = LongHashHelpers.GetPrime(capacity);
            Resize(newSize, forceNewHashCodes: false);
            return newSize;
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if 
        /// it had been originally initialized with all its entries.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        ///
        /// To allocate minimum size storage array, execute the following statements:
        ///
        /// dictionary.Clear();
        /// dictionary.TrimExcess();
        /// </remarks>
        public void TrimExcess()
        {
            TrimExcess(Count);
        }

        /// <summary>
        /// Sets the capacity of this dictionary to hold up 'capacity' entries
        /// without any further expansion of its backing storage
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize the memory overhead
        /// once it is known that no new elements will be added.
        /// </remarks>
        public void TrimExcess(int capacity)
        {
            if (capacity < Count)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Entry[]? oldEntries = _entries;
            int newSize = LongHashHelpers.GetPrime(capacity);
            int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
            if (newSize >= currentCapacity)
                return;

            int oldCount = _count;
            _version++;
            Initialize(newSize);
            Entry[]? entries = _entries;
            int count = 0;
            for (int i = 0; i < oldCount; i++)
            {
                // At this point, we know we have entries.
                if (oldEntries![i].Next >= -1)
                {
                    ref Entry entry = ref entries![count];
                    entry = oldEntries[i];

                    long hashCode = oldEntries![i].HashCode;
                    ref int bucket = ref GetBucket(hashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = count + 1;
                    count++;
                }
            }

            _count = count;
            _freeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(long hashCode)
        {
            int[] buckets = _buckets!;
            return ref buckets[hashCode % buckets.LongLength];
        }

        private struct Entry
        {
            public long HashCode;

            /// <summary>
            /// 0-based index of next entry in chain: -1 means end of chain
            /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int Next;

            public TKey Key;
            public TValue Value;
        }
    }
}