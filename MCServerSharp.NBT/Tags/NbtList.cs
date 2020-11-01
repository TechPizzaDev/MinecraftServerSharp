using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtList<TTag> : NbtContainer<TTag, List<TTag>.Enumerator>, IList<TTag>
        where TTag : NbTag
    {
        public List<TTag> Items { get; }

        public override int Count => Items.Count;
        public override NbtType Type => NbtType.List;

        public bool IsReadOnly => false;

        public TTag this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public NbtList(int capacity)
        {
            Items = new List<TTag>(capacity);
        }

        public NbtList()
        {
            Items = new List<TTag>();
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            var elementType = GetNbtType(typeof(TTag));
            writer.Write((byte)elementType);

            writer.Write(Count);

            foreach (var item in Items)
                item.WritePayload(writer, flags & NbtFlags.Endianness);
        }

        public override List<TTag>.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public NbtList<TTag> Add(TTag tag)
        {
            Items.Add(tag);
            return this;
        }

        void ICollection<TTag>.Add(TTag item)
        {
            Items.Add(item);
        }

        public int IndexOf(TTag item)
        {
            return ((IList<TTag>)Items).IndexOf(item);
        }

        public void Insert(int index, TTag item)
        {
            ((IList<TTag>)Items).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<TTag>)Items).RemoveAt(index);
        }

        public void Clear()
        {
            ((ICollection<TTag>)Items).Clear();
        }

        public bool Contains(TTag item)
        {
            return ((ICollection<TTag>)Items).Contains(item);
        }

        public void CopyTo(TTag[] array, int arrayIndex)
        {
            ((ICollection<TTag>)Items).CopyTo(array, arrayIndex);
        }

        public bool Remove(TTag item)
        {
            return ((ICollection<TTag>)Items).Remove(item);
        }
    }
}
