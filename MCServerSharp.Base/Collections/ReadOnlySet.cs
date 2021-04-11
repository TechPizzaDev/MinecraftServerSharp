using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MCServerSharp.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlySet<T> : IReadOnlySet<T>
    {
        private static ReadOnlySet<T>? _empty;

        public static ReadOnlySet<T> Empty
        {
            get
            {
                if (_empty == null)
                    // we don't care about threading; concurrency can only cause some extra allocs here
                    _empty = new ReadOnlySet<T>(new HashSet<T>());

                return _empty;
            }
        }

        private readonly IReadOnlySet<T> _set;

        public int Count => _set.Count;

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses an <see cref="IReadOnlySet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(IReadOnlySet<T> set)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses an <see cref="ISet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(ISet<T> set)
        {
            _set = new SetWrapper(set);
        }

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses a <see cref="HashSet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(HashSet<T> set) : this((IReadOnlySet<T>)set)
        {
        }

        /// <summary>
        /// Constructs an immutable <see cref="ReadOnlySet{T}"/>
        /// by copying elements from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable whose elements are copied from.</param>
        /// <param name="comparer">
        /// The comparer to use when comparing values in the set,
        /// or <see langword="null"/> to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        public ReadOnlySet(IEnumerable<T> enumerable, IEqualityComparer<T>? comparer)
        {
            _set = new HashSet<T>(enumerable, comparer);
        }

        public ReadOnlySet(IEnumerable<T> enumerable) : this(enumerable, null)
        {
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_set);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private HashSet<T>.Enumerator _hashSetEnumerator;
            private IEnumerator<T> _genericEnumerator;
            private IEnumerator? _boxedCache;

            internal Enumerator(IEnumerable<T> enumerable)
            {
                if (enumerable is HashSet<T> hashSet)
                {
                    _hashSetEnumerator = hashSet.GetEnumerator();
                    _genericEnumerator = null!;
                }
                else
                {
                    _hashSetEnumerator = default;
                    _genericEnumerator = enumerable.GetEnumerator();
                }
                _boxedCache = null;
            }

            public T Current
            {
                get
                {
                    if (_genericEnumerator != null)
                        return _genericEnumerator.Current;
                    return _hashSetEnumerator.Current;
                }
            }

            object? IEnumerator.Current
            {
                get
                {
                    if (_genericEnumerator != null)
                        return _genericEnumerator.Current;
                    return GetBoxedCache().Current;
                }
            }

            public bool MoveNext()
            {
                if (_genericEnumerator != null)
                    return _genericEnumerator.MoveNext();
                return _hashSetEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                if (_genericEnumerator != null)
                    _genericEnumerator.Reset();
                else
                    GetBoxedCache().Reset();
            }

            public void Dispose()
            {
                if (_genericEnumerator != null)
                    _genericEnumerator.Dispose();
                else
                    _hashSetEnumerator.Dispose();
            }

            private IEnumerator GetBoxedCache()
            {
                if (_boxedCache == null)
                    _boxedCache = _hashSetEnumerator;
                return _boxedCache;
            }
        }

        private class SetWrapper : IReadOnlySet<T>
        {
            public ISet<T> Set { get; }

            public SetWrapper(ISet<T> set)
            {
                Set = set ?? throw new ArgumentNullException(nameof(set));
            }

            public int Count => Set.Count;

            public bool Contains(T item) => Set.Contains(item);

            public IEnumerator<T> GetEnumerator() => Set.GetEnumerator();

            public bool IsProperSubsetOf(IEnumerable<T> other) => Set.IsProperSubsetOf(other);

            public bool IsProperSupersetOf(IEnumerable<T> other) => Set.IsProperSupersetOf(other);

            public bool IsSubsetOf(IEnumerable<T> other) => Set.IsSubsetOf(other);

            public bool IsSupersetOf(IEnumerable<T> other) => Set.IsSupersetOf(other);

            public bool Overlaps(IEnumerable<T> other) => Set.Overlaps(other);

            public bool SetEquals(IEnumerable<T> other) => Set.SetEquals(other);

            IEnumerator IEnumerable.GetEnumerator() => Set.GetEnumerator();
        }
    }
}
