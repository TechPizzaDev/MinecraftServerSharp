using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.NBT
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T">The element type of this container.</typeparam>
    /// <typeparam name="TEnumerator">
    /// Generic enumerator type to allow implementations to provide a struct enumerator to reduce garbage allocations.
    /// </typeparam>
    public abstract class NbtContainer<T, TEnumerator> : NbTag, IReadOnlyCollection<T>
        where TEnumerator : IEnumerator<T>
    {
        public abstract int Count { get; }
        
        public NbtContainer()
        {
        }

        public abstract TEnumerator GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
