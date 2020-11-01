using System;
using System.Collections.Generic;
using MCServerSharp.Collections;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbtArray<T> : NbtContainer<T, ArrayEnumerator<T>>, IReadOnlyList<T>
    {
        private T[] _items;

        public T[] Items
        {
            get => _items;
            set => _items = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override int Count => Items.Length;

        public ref T this[int index] => ref Items[index];

        T IReadOnlyList<T>.this[int index] => Items[index];

        public NbtArray(T[] items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public NbtArray(int count) : this(new T[count])
        {
        }

        public Memory<T> AsMemory()
        {
            return Items;
        }

        public Span<T> AsSpan()
        {
            return Items;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Count);
        }

        public override ArrayEnumerator<T> GetEnumerator()
        {
            return Items;
        }
    }
}
