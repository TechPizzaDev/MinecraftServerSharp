using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtList<TTag> : NbtContainer<TTag, List<TTag>.Enumerator>
        where TTag : NbTag
    {
        protected List<TTag> Items { get; }

        public override int Count => Items.Count;
        public override NbtType Type => NbtType.List;

        public NbtList(Utf8String? name, int capacity) : base(name)
        {
            Items = new List<TTag>(capacity);
        }

        public NbtList(Utf8String? name) : base(name)
        {
            Items = new List<TTag>();
        }

        public NbtList<TTag> Add(TTag tag)
        {
            Items.Add(tag);
            return this;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Count);
        }

        public override List<TTag>.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
