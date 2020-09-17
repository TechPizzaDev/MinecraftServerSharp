using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.NBT
{
    public abstract class NbtContainer<T, TEnumerator> : NbTag, IReadOnlyCollection<T>
        where TEnumerator : IEnumerator<T>
    {
        public abstract int Count { get; }
        
        public NbtContainer(Utf8String? name) : base(name)
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
