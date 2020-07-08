using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinecraftServerSharp.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlySet<T> : IReadOnlySet<T>, ISet<T>
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

        private readonly ISet<T>? _set;
        private readonly IReadOnlySet<T>? _roSet;

        public bool IsReadOnly => true;

        public int Count => _set != null ? _set.Count : _roSet!.Count;

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses an <see cref="ISet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(ISet<T> set)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses an <see cref="IReadOnlySet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(IReadOnlySet<T> set)
        {
            _roSet = set ?? throw new ArgumentNullException(nameof(set));
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
            return _set != null ? _set.Contains(item) : _roSet!.Contains(item);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set != null ? _set.IsProperSubsetOf(other) : _roSet!.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set != null ? _set.IsProperSupersetOf(other) : _roSet!.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set != null ? _set.IsSubsetOf(other) : _roSet!.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set != null ? _set.IsSupersetOf(other) : _roSet!.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set != null ? _set.Overlaps(other) : _roSet!.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set != null ? _set.SetEquals(other) : _roSet!.SetEquals(other);
        }

        public Enumerator GetEnumerator()
        {
            return _set != null ? new Enumerator(_set) : new Enumerator(_roSet!);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set != null ? _set.GetEnumerator() : _roSet!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set != null ? _set.GetEnumerator() : _roSet!.GetEnumerator();
        }

        #region ISet<T> (and ICollection<T>)

        bool ISet<T>.Add(T item)
        {
            throw new InvalidOperationException();
        }

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new InvalidOperationException();
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new InvalidOperationException();
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new InvalidOperationException();
        }

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new InvalidOperationException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new InvalidOperationException();
        }

        void ICollection<T>.Clear()
        {
            throw new InvalidOperationException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new InvalidOperationException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new InvalidOperationException();
        }

        #endregion

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
    }
}
