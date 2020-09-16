using System;
using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtCompound : NbtContainer<NbtCompound>
    {
        private Dictionary<Utf8String, NbTag> _children;

        public override NbtType Type => NbtType.Compound;
        public override int Count => _children.Count;

        public NbtCompound(Utf8String? name = null) : base(name)
        {
            _children = new Dictionary<Utf8String, NbTag>();
        }

        public override NbtCompound Add(NbTag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (tag.Name == null)
                throw new ArgumentException("The tag is not named.");

            _children.Add(tag.Name, tag);
            return this;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            foreach (var child in _children)
                child.Value.Write(writer, NbtFlags.TypedNamed);

            NbtEnd.Instance.Write(writer, NbtFlags.Typed);
        }
    }
}
