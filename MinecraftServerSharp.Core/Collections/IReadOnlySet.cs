using System.Collections.Generic;

namespace MinecraftServerSharp.Collections
{
    // TODO: remove when NET5 hits
    public interface IReadOnlySet<T> : IReadOnlyCollection<T>, IEnumerable<T>
    {
        /// <summary>
        /// Determines whether the set contains a specific value.
        /// </summary>
        bool Contains(T item);

        /// <summary>
        /// Determines whether the set is a proper (strict) subset of a specified enumerable.
        /// </summary>
        bool IsProperSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the set is a proper (strict) superset of a specified enumerable.
        /// </summary>
        bool IsProperSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the set is a subset of a specified enumerable.
        /// </summary>
        bool IsSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the set is a superset of a specified enumerable.
        /// </summary>
        bool IsSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the set overlaps with the specified enumerable.
        /// </summary>
        bool Overlaps(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the set and the specified enumerable contain the same elements.
        /// </summary>
        bool SetEquals(IEnumerable<T> other);
    }
}
