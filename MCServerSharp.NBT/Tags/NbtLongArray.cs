using System;
using System.Collections;
using System.Collections.Generic;
using MCServerSharp.Data.IO;
using MCServerSharp.Memory;

namespace MCServerSharp.NBT
{
    public class NbtLongArray : NbTag, INbtArray<long>
    {
        protected virtual ReadOnlyMemory<long> Items { get; }

        public int Count => Items.Length;

        long IReadOnlyList<long>.this[int index] => Items.Span[index];

        public ref readonly long this[int index] => ref Items.Span[index];

        public override NbtType Type => NbtType.LongArray;

        public NbtLongArray(ReadOnlyMemory<long> items)
        {
            Items = items;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Items.Length);
            writer.Write(Items.Span);
        }

        public ReadOnlyMemory<long> AsMemory()
        {
            return Items;
        }

        public ReadOnlySpan<long> AsSpan()
        {
            return Items.Span;
        }

        public MemoryEnumerable<long> GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator<long> IEnumerable<long>.GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerable();
        }
    }

    public class NbtMutLongArray : NbtLongArray, INbtMutArray<long>
    {
        public Memory<long> Buffer { get; set; }

        protected override ReadOnlyMemory<long> Items => Buffer;

        public new ref long this[int index] => ref Buffer.Span[index];

        public NbtMutLongArray() : base(ReadOnlyMemory<long>.Empty)
        {
        }

        public NbtMutLongArray(Memory<long> items) : this()
        {
            Buffer = items;
        }

        public NbtMutLongArray(int count) : this(new long[count])
        {
        }

        public new Memory<long> AsMemory()
        {
            return Buffer;
        }

        public new Span<long> AsSpan()
        {
            return Buffer.Span;
        }
    }
}
