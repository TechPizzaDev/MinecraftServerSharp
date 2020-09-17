using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    using Dict = Dictionary<Utf8String, NbTag>;
    using KeyValue = KeyValuePair<Utf8String, NbTag>;

    public class NbtCompound :
        NbtContainer<KeyValue, Dict.Enumerator>,
        IReadOnlyDictionary<Utf8String, NbTag>
    {
        protected Dict Items { get; }

        public override int Count => Items.Count;
        public override NbtType Type => NbtType.Compound;

        public Dict.KeyCollection Keys => Items.Keys;
        public Dict.ValueCollection Values => Items.Values;

        IEnumerable<Utf8String> IReadOnlyDictionary<Utf8String, NbTag>.Keys => Keys;
        IEnumerable<NbTag> IReadOnlyDictionary<Utf8String, NbTag>.Values => Values;

        public NbTag this[Utf8String key] => Items[key];

        public NbtCompound(Utf8String? name = null) : base(name)
        {
            Items = new Dict();
        }

        public NbtCompound Add(NbTag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            if (tag.Name == null)
                throw new ArgumentException("The tag is not named.");

            Items.Add(tag.Name, tag);
            return this;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            foreach (var item in Items)
                item.Value.Write(writer, NbtFlags.TypedNamed);

            NbtEnd.Instance.Write(writer, NbtFlags.Typed);
        }

        public bool ContainsKey(Utf8String key)
        {
            return Items.ContainsKey(key);
        }

        public bool TryGetValue(Utf8String key, [MaybeNullWhen(false)] out NbTag value)
        {
            return Items.TryGetValue(key, out value);
        }

        public override Dict.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator<KeyValue> IEnumerable<KeyValue>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
