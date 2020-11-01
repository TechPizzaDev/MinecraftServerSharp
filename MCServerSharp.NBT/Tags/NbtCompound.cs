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
        IDictionary<Utf8String, NbTag>,
        IReadOnlyDictionary<Utf8String, NbTag>
    {
        public Dict Items { get; }
        public Utf8String? Name { get; set; }

        public override int Count => Items.Count;
        public override NbtType Type => NbtType.Compound;

        public Dict.KeyCollection Keys => Items.Keys;
        public Dict.ValueCollection Values => Items.Values;

        ICollection<Utf8String> IDictionary<Utf8String, NbTag>.Keys => Keys;
        ICollection<NbTag> IDictionary<Utf8String, NbTag>.Values => Values;

        IEnumerable<Utf8String> IReadOnlyDictionary<Utf8String, NbTag>.Keys => Keys;
        IEnumerable<NbTag> IReadOnlyDictionary<Utf8String, NbTag>.Values => Values;

        public bool IsReadOnly => false;

        public NbTag this[Utf8String key]
        {
            get => Items[key];
            set => Items[key] = value;
        }

        public NbtCompound(Utf8String? name = null)
        {
            Name = name;
            Items = new Dict();
        }

        public override void WriteHeader(NetBinaryWriter writer, NbtFlags flags)
        {
            base.WriteHeader(writer, flags);

            if (flags.HasFlags(NbtFlags.Named))
            {
                if (Name == null)
                {
                    writer.Write((ushort)0);
                }
                else
                {
                    writer.Write((ushort)Name.Length);
                    writer.WriteRaw(Name);
                }
            }
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            foreach (var (name, value) in Items)
            {
                value.WriteHeader(writer, NbtFlags.Typed);

                writer.Write((ushort)name.Length);
                writer.WriteRaw(name);

                value.WritePayload(writer, NbtFlags.Typed);
            }

            End.WriteHeader(writer, NbtFlags.Typed);
        }

        public override Dict.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public NbtCompound Add(Utf8String name, NbTag value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Items.Add(name, value);
            return this;
        }

        public NbtCompound Add(string name, NbTag value)
        {
            return Add((Utf8String)name!, value);
        }

        void IDictionary<Utf8String, NbTag>.Add(Utf8String name, NbTag value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Items.Add(name, value);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool ContainsKey(Utf8String name)
        {
            return Items.ContainsKey(name);
        }

        public bool TryGetValue(Utf8String name, [MaybeNullWhen(false)] out NbTag value)
        {
            return Items.TryGetValue(name, out value);
        }

        public bool TryAdd(Utf8String name, NbTag value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Items.TryAdd(name, value);
        }

        public bool Remove(Utf8String name)
        {
            return Items.Remove(name);
        }

        public void Add(KeyValue item)
        {
            ((ICollection<KeyValue>)Items).Add(item);
        }

        public bool Contains(KeyValue item)
        {
            return ((ICollection<KeyValue>)Items).Contains(item);
        }

        public void CopyTo(KeyValue[] array, int arrayIndex)
        {
            ((ICollection<KeyValue>)Items).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValue item)
        {
            return ((ICollection<KeyValue>)Items).Remove(item);
        }

        IEnumerator<KeyValue> IEnumerable<KeyValue>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
