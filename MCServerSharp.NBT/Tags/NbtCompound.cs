using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    using Dict = Dictionary<Utf8String, NbTag>;
    using KeyValue = KeyValuePair<Utf8String, NbTag>;

    public class NbtCompound :
        NbTag,
        IReadOnlyDictionary<Utf8String, NbTag>
    {
        private static Dict EmptyItems { get; } = new();

        public static NbtCompound Empty { get; } = new(null, new Dict());

        protected Dict Items { get; set; }

        public Utf8String? Name { get; protected set; }

        public int Count => Items.Count;
        public Dict.KeyCollection Keys => Items.Keys;
        public Dict.ValueCollection Values => Items.Values;

        IEnumerable<Utf8String> IReadOnlyDictionary<Utf8String, NbTag>.Keys => Items.Keys;
        IEnumerable<NbTag> IReadOnlyDictionary<Utf8String, NbTag>.Values => Items.Values;

        public NbTag this[Utf8String key]
        {
            get => Items[key];
            set => Items[key] = value;
        }

        public override NbtType Type => NbtType.Compound;

        protected NbtCompound(Utf8String? name, Dict items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Name = name;
        }

        public NbtCompound(Utf8String? name) : this(name, EmptyItems)
        {
        }

        public NbtCompound(Utf8String? name, IEnumerable<KeyValue> items) : this(name, new Dict(items))
        {
        }

        public NbtCompound() : this(null, EmptyItems)
        {
        }

        public override void WriteHeader(NetBinaryWriter writer, NbtFlags flags)
        {
            base.WriteHeader(writer, flags);

            if (flags.HasFlags(NbtFlags.Named))
            {
                Utf8String? name = Name;
                if (name == null || name.Length == 0)
                {
                    writer.Write((ushort)0);
                }
                else
                {
                    writer.Write((ushort)name.Length);
                    writer.WriteRaw(name);
                }
            }
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            foreach (var (name, value) in Items)
            {
                if (value != null)
                {
                    value.WriteHeader(writer, NbtFlags.Typed);

                    writer.Write((ushort)name.Length);
                    writer.WriteRaw(name);

                    value.WritePayload(writer, NbtFlags.Typed);
                }
            }

            NbtEnd.Write(writer, NbtFlags.Typed);
        }

        public bool ContainsKey(Utf8String name)
        {
            return Items.ContainsKey(name);
        }

        public bool TryGetValue(Utf8String name, [MaybeNullWhen(false)] out NbTag value)
        {
            return Items.TryGetValue(name, out value);
        }

        public bool Contains(KeyValue item)
        {
            return ((ICollection<KeyValue>)Items).Contains(item);
        }

        public void CopyTo(KeyValue[] array, int arrayIndex)
        {
            ((ICollection<KeyValue>)Items).CopyTo(array, arrayIndex);
        }

        public Dict.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator<KeyValue> IEnumerable<KeyValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class NbtMutCompound :
        NbtCompound,
        IDictionary<Utf8String, NbTag>
    {
        public new Dict Items
        {
            get => base.Items;
            set => base.Items = value ?? throw new ArgumentNullException(nameof(value));
        }

        public new Utf8String? Name
        {
            get => base.Name;
            set => base.Name = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool IsReadOnly => false;

        ICollection<Utf8String> IDictionary<Utf8String, NbTag>.Keys => Keys;
        ICollection<NbTag> IDictionary<Utf8String, NbTag>.Values => Values;

        public NbtMutCompound(Utf8String? name, Dict items) : base(name, items)
        {
        }

        public NbtMutCompound(Utf8String? name) : base(name, new Dict())
        {
        }

        public NbtMutCompound(Utf8String? name, IEnumerable<KeyValue> items) : base(name, items)
        {
        }

        public NbtMutCompound() : base(null, new Dict())
        {
        }

        void IDictionary<Utf8String, NbTag>.Add(Utf8String name, NbTag value)
        {
            Items.Add(name, value);
        }

        public bool TryAdd(Utf8String name, NbTag value)
        {
            return Items.TryAdd(name, value);
        }

        public NbtMutCompound Add(Utf8String name, NbTag value)
        {
            Items.Add(name, value);
            return this;
        }

        public NbtMutCompound Add(string name, NbTag value)
        {
            return Add((Utf8String)name, value);
        }

        public bool Remove(Utf8String name)
        {
            return Items.Remove(name);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void Add(KeyValue item)
        {
            ((ICollection<KeyValue>)Items).Add(item);
        }

        public bool Remove(KeyValue item)
        {
            return ((ICollection<KeyValue>)Items).Remove(item);
        }
    }
}
