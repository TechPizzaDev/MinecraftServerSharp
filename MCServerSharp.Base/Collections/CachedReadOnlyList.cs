using System;
using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    /// <summary>
    /// Caches a <see cref="ReadOnlyList{T}"/> and it's source to mitigate allocation from 
    /// repetitive <see cref="List{T}.AsReadOnly"/> calls.
    /// </summary>
    public readonly struct CachedReadOnlyList<T> : IEnumerable<T>
    {
        public List<T> Source { get; }
        public ReadOnlyList<T> ReadOnly { get; }

        public bool IsEmpty => Source == null;

        public CachedReadOnlyList(List<T> items)
        {
            Source = items ?? throw new ArgumentNullException(nameof(items));
            ReadOnly = Source.AsReadOnlyList();
        }

        /// <summary>
        /// Creates a <see cref="CachedReadOnlyList{T}"/> with a new empty list.
        /// </summary>
        public static CachedReadOnlyList<T> Create()
        {
            return new CachedReadOnlyList<T>(new List<T>());
        }

        public List<T>.Enumerator GetEnumerator() => ReadOnly.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ReadOnly.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ReadOnly.GetEnumerator();
    }
}
