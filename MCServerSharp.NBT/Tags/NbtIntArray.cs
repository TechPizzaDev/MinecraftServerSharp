using System;
using System.Collections;
using System.Collections.Generic;
using MCServerSharp.Data.IO;
using MCServerSharp.Memory;

namespace MCServerSharp.NBT
{
    public class NbtIntArray : NbTag, INbtArray<int>
    {
        protected virtual ReadOnlyMemory<int> Items { get; }

        public int Count => Items.Length;

        int IReadOnlyList<int>.this[int index] => Items.Span[index];

        public ref readonly int this[int index] => ref Items.Span[index];

        public override NbtType Type => NbtType.IntArray;

        public NbtIntArray(ReadOnlyMemory<int> items)
        {
            Items = items;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Items.Length);
            writer.Write(Items.Span);
        }

        public ReadOnlyMemory<int> AsMemory()
        {
            return Items;
        }

        public ReadOnlySpan<int> AsSpan()
        {
            return Items.Span;
        }

        public MemoryEnumerable<int> GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerable();
        }
    }

    public class NbtMutIntArray : NbtIntArray, INbtMutArray<int>
    {
        public Memory<int> Buffer { get; set; }

        protected override ReadOnlyMemory<int> Items => Buffer;

        public new ref int this[int index] => ref Buffer.Span[index];

        public NbtMutIntArray() : base(ReadOnlyMemory<int>.Empty)
        {
        }

        public NbtMutIntArray(Memory<int> items) : this()
        {
            Buffer = items;
        }

        public NbtMutIntArray(int count) : this(new int[count])
        {
        }

        public new Memory<int> AsMemory()
        {
            return Buffer;
        }

        public new Span<int> AsSpan()
        {
            return Buffer.Span;
        }
    }
}
