using System;
using System.Collections;
using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtList<TTag> : NbTag, IReadOnlyList<TTag>
        where TTag : NbTag
    {
        private static List<TTag> EmptyItems { get; } = new();

        public static NbtList<TTag> Empty => new();

        protected List<TTag> Items { get; set; }

        public int Count => Items.Count;
        public bool IsReadOnly => false;

        public TTag this[int index] => Items[index];

        public override NbtType Type => NbtType.List;

        protected NbtList(List<TTag> items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public NbtList(IEnumerable<TTag> items) : this(new List<TTag>(items))
        {
        }

        public NbtList() : this(EmptyItems)
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            NbtType elementType = GetNbtType<TTag>();
            writer.Write((byte)elementType);

            writer.Write(Count);

            foreach (TTag item in Items)
            {
                if (item != null)
                    item.WritePayload(writer, NbtFlags.None);
            }
        }

        public int IndexOf(TTag item)
        {
            return Items.IndexOf(item);
        }

        public bool Contains(TTag item)
        {
            return Items.Contains(item);
        }

        public void CopyTo(TTag[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public List<TTag>.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator<TTag> IEnumerable<TTag>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class NbtMutList<TTag> : NbtList<TTag>, IList<TTag>
        where TTag : NbTag
    {
        public new List<TTag> Items
        {
            get => base.Items;
            set => base.Items = value ?? throw new ArgumentNullException(nameof(value));
        }

        public new TTag this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public NbtMutList(List<TTag> items) : base(items)
        {
        }

        public NbtMutList(IEnumerable<TTag> items) : base(items)
        {
        }

        public NbtMutList() : base(new List<TTag>())
        {
        }

        public NbtMutList<TTag> Add(TTag tag)
        {
            Items.Add(tag);
            return this;
        }

        void ICollection<TTag>.Add(TTag item)
        {
            Items.Add(item);
        }

        public void Insert(int index, TTag item)
        {
            Items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Remove(TTag item)
        {
            return Items.Remove(item);
        }
    }
}
