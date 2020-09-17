using System.Collections.Generic;
using MCServerSharp.Collections;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbtArray<T> : NbtContainer<T, ArrayEnumerator<T>>, IReadOnlyList<T>
    {
        protected T[] Items { get; }
        
        public override int Count => Items.Length;

        public ref T this[int index] => ref Items[index];

        T IReadOnlyList<T>.this[int index] => Items[index];

        public NbtArray(Utf8String? name, int count) : base(name)
        {
            Items = new T[count];
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Count);
        }

        public override ArrayEnumerator<T> GetEnumerator()
        {
            return Items;
        }
    }
}
