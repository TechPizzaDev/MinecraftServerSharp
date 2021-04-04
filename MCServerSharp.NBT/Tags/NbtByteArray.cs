using System;
using System.Collections;
using System.Collections.Generic;
using MCServerSharp.Data.IO;
using MCServerSharp.Memory;

namespace MCServerSharp.NBT
{
    public class NbtByteArray : NbTag, INbtArray<sbyte>
    {
        protected virtual ReadOnlyMemory<sbyte> Items { get; }

        public int Count => Items.Length;

        sbyte IReadOnlyList<sbyte>.this[int index] => Items.Span[index];

        public ref readonly sbyte this[int index] => ref Items.Span[index];

        public override NbtType Type => NbtType.ByteArray;

        public NbtByteArray(ReadOnlyMemory<sbyte> items)
        {
            Items = items;
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            writer.Write(Items.Length);
            writer.Write(Items.Span);
        }

        public ReadOnlyMemory<sbyte> AsMemory()
        {
            return Items;
        }

        public ReadOnlySpan<sbyte> AsSpan()
        {
            return Items.Span;
        }

        public MemoryEnumerable<sbyte> GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator<sbyte> IEnumerable<sbyte>.GetEnumerator()
        {
            return Items.GetEnumerable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerable();
        }
    }

    public class NbtMutByteArray : NbtByteArray, INbtMutArray<sbyte>
    {
        public Memory<sbyte> Buffer { get; set; }

        protected override ReadOnlyMemory<sbyte> Items => Buffer;

        public new ref sbyte this[int index] => ref Buffer.Span[index];

        public NbtMutByteArray() : base(ReadOnlyMemory<sbyte>.Empty)
        {
        }

        public NbtMutByteArray(Memory<sbyte> items) : this()
        {
            Buffer = items;
        }

        public NbtMutByteArray(int count) : this(new sbyte[count])
        {
        }

        public new Memory<sbyte> AsMemory()
        {
            return Buffer;
        }

        public new Span<sbyte> AsSpan()
        {
            return Buffer.Span;
        }
    }
}
