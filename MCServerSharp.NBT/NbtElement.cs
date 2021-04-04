using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MCServerSharp.NBT
{
    [DebuggerDisplay("{ToString(), nq}")]
    public readonly partial struct NbtElement
    {
        private readonly NbtDocument _parent;
        private readonly int _index;

        /// <summary>
        /// Gets an element within this container.
        /// </summary>
        /// <exception cref="InvalidOperationException">This element is not a container.</exception>
        public NbtElement this[int index] => GetContainerElement(index);

        public NbtElement this[ReadOnlySpan<byte> utf8Name] => GetCompoundElement(utf8Name);

        public NbtElement this[ReadOnlySpan<char> name] => GetCompoundElement(name);

        public NbtElement this[string name] => this[name.AsSpan()];

        public NbtType Type => _parent?.GetTagType(_index) ?? NbtType.Undefined;

        public NbtFlags Flags => _parent?.GetFlags(_index) ?? NbtFlags.None;

        public ReadOnlyMemory<byte> Name => _parent != null
            ? _parent.GetTagName(_index)
            : ReadOnlyMemory<byte>.Empty;

        internal NbtElement[]? Children
        {
            get
            {
                if (Type.IsContainer())
                    return EnumerateContainer().ToArray();
                return null;
            }
        }

        // TODO: add debug tree view

        internal NbtElement(NbtDocument parent, int index)
        {
            // parent is usually not null, but the Current property
            // on the enumerators (when initialized as default) can
            // get here with a null.
            Debug.Assert(index >= 0);

            _parent = parent;
            _index = index;
        }

        public NbtType GetBaseType()
        {
            AssertValidInstance();

            NbtType type = Type;
            return type switch
            {
                NbtType.List => _parent.GetListType(_index),
                NbtType.ByteArray => NbtType.Byte,
                NbtType.IntArray => NbtType.Int,
                NbtType.LongArray => NbtType.Long,
                _ => type,
            };
        }

        /// <summary>
        /// Gets an element within this container.
        /// </summary>
        /// <exception cref="InvalidOperationException">This element is not a container.</exception>
        public NbtElement GetContainerElement(int index)
        {
            AssertValidInstance();
            return _parent.GetContainerElement(_index, index);
        }

        public bool TryGetCompoundElement(
            ReadOnlySpan<byte> utf8Name, out NbtElement element,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            AssertValidInstance();
            if (Type != NbtType.Compound)
                throw new InvalidOperationException("The tag is not a compound.");

            foreach (NbtElement item in EnumerateContainer())
            {
                if (Utf8String.Equals(utf8Name, item.Name.Span, comparison))
                {
                    element = item;
                    return true;
                }
            }
            element = default;
            return false;
        }

        // TODO: replace with Utf8Span
        public NbtElement GetCompoundElement(
            ReadOnlySpan<byte> utf8Name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (TryGetCompoundElement(utf8Name, out NbtElement element, comparison))
            {
                return element;
            }
            throw new KeyNotFoundException();
        }

        public bool TryGetCompoundElement(
            ReadOnlySpan<char> name,
            out NbtElement element,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            AssertValidInstance();
            if (Type != NbtType.Compound)
                throw new InvalidOperationException("The tag is not a compound.");

            foreach (NbtElement item in EnumerateContainer())
            {
                if (Utf8String.Equals(name, item.Name.Span, comparison))
                {
                    element = item;
                    return true;
                }
            }
            element = default;
            return false;
        }

        public NbtElement GetCompoundElement(
            ReadOnlySpan<char> name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (TryGetCompoundElement(name, out NbtElement element, comparison))
            {
                return element;
            }
            throw new KeyNotFoundException();
        }

        public ReadOnlyMemory<byte> GetRawData()
        {
            AssertValidInstance();
            return _parent.GetRawData(_index);
        }

        public ReadOnlyMemory<byte> GetArrayData(out NbtType tagType)
        {
            AssertValidInstance();
            return _parent.GetArrayData(_index, out tagType);
        }

        public int GetArrayElementSize()
        {
            return _parent.GetArrayElementSize(_index);
        }

        /// <summary>
        /// Gets the number of elements contained within the current collection element.
        /// </summary>
        /// <returns>The number of elements contained within the current element.</returns>
        /// <exception cref="InvalidOperationException">This element is not a collection.</exception>
        /// <exception cref="ObjectDisposedException">The parent <see cref="NbtDocument"/> has been disposed.</exception>
        public int GetLength()
        {
            AssertValidInstance();
            return _parent.GetLength(_index);
        }

        public sbyte GetByte()
        {
            AssertValidInstance();
            return _parent.GetByte(_index);
        }

        public short GetShort()
        {
            AssertValidInstance();
            return _parent.GetShort(_index);
        }

        public int GetInt()
        {
            AssertValidInstance();
            return _parent.GetInt(_index);
        }

        public long GetLong()
        {
            AssertValidInstance();
            return _parent.GetLong(_index);
        }

        public float GetFloat()
        {
            AssertValidInstance();
            return _parent.GetFloat(_index);
        }

        public double GetDouble()
        {
            AssertValidInstance();
            return _parent.GetDouble(_index);
        }

        public string GetString()
        {
            AssertValidInstance();
            return _parent.GetString(_index);
        }

        public ContainerEnumerator EnumerateContainer()
        {
            AssertValidInstance();

            if (!Type.IsContainer())
                throw new InvalidOperationException("This element is not a container.");

            return new ContainerEnumerator(this);
        }

        public ArrayEnumerator<T> EnumerateArray<T>()
            where T : unmanaged
        {
            AssertValidInstance();

            if (!Type.IsArray())
                throw new InvalidOperationException("This element is not an array.");

            return new ArrayEnumerator<T>(this);
        }

        public void WriteTo(NbtWriter writer)
        {
            AssertValidInstance();

            _parent.WriteTagTo(_index, writer);
        }

        /// <summary>
        ///   Get a <see cref="NbtElement"/> which can be safely stored beyond the 
        ///   lifetime of the original <see cref="NbtDocument"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="NbtElement"/> which can be safely stored beyond the 
        ///   lifetime of the original <see cref="NbtDocument"/>.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     If this <see cref="NbtElement"/> is itself the output of a previous clone,
        ///     this method results in no additional memory allocation.
        ///   </para>
        /// </remarks>
        public NbtElement Clone()
        {
            AssertValidInstance();

            if (!_parent.IsDisposable)
                return this;

            return _parent.CloneTag(_index);
        }

        /// <summary>
        ///   Compares <paramref name="text" /> to the string value of this element.
        /// </summary>
        /// <param name="text">The text to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the string value of this element matches <paramref name="text"/>,
        ///   <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">This element is not a <see cref="NbtType.String"/>.</exception>
        /// <remarks>
        ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
        ///   the result of calling <see cref="GetString" />, but avoids creating the string instance.
        /// </remarks>
        public bool StringEquals(string? text)
        {
            return StringEquals(text.AsSpan());
        }

        /// <summary>
        ///   Compares <paramref name="data" /> to the value of this element, either an array or UTF-8 text.
        /// </summary>
        /// <param name="data">The data to compare against, either an array or UTF-8 text.</param>
        /// <returns>
        ///   <see langword="true" /> if the value of this element is sequence equal to
        ///   <paramref name="data" />, <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// This element is not a <see cref="NbtType.String"/> or array.
        /// </exception>
        /// <remarks>
        /// This method is endianness-sensitive for <see cref="NbtType.IntArray"/> and <see cref="NbtType.LongArray"/>,
        /// meaning that <paramref name="data"/> needs to have the same endianness as the element.
        /// </remarks>
        public bool SequenceEqual(ReadOnlySpan<byte> data)
        {
            AssertValidInstance();

            return _parent.ArraySequenceEqual(_index, data);
        }

        /// <summary>
        ///   Compares <paramref name="text" /> to the string value of this element.
        /// </summary>
        /// <param name="text">The text to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the string value of this element matches <paramref name="text"/>,
        ///   <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">This element is not a <see cref="NbtType.String"/>.</exception>
        /// <remarks>
        ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
        ///   the result of calling <see cref="GetString" />, but avoids creating the string instance.
        /// </remarks>
        public bool StringEquals(ReadOnlySpan<char> text)
        {
            AssertValidInstance();

            return _parent.StringEquals(_index, text);
        }

        private void AssertValidInstance()
        {
            if (_parent == null)
                throw new InvalidOperationException();
        }

        public override string ToString()
        {
            var name = Name.ToUtf8String();
            var builder = new StringBuilder(name.Length + 20);

            if (Flags.HasFlag(NbtFlags.Named))
                builder.Append('"').Append(name).Append("\": ");

            var tagType = Type;
            switch (tagType)
            {
                case NbtType.String:
                    builder.Append('"').Append(GetString()).Append('"');
                    break;

                case NbtType.Byte:
                    builder.Append(GetByte()).Append('b');
                    break;

                case NbtType.Short:
                    builder.Append(GetShort()).Append('s');
                    break;

                case NbtType.Int:
                    builder.Append(GetInt()).Append('i');
                    break;

                case NbtType.Long:
                    builder.Append(GetLong()).Append('l');
                    break;

                case NbtType.Float:
                    builder.Append(GetFloat()).Append('f');
                    break;

                case NbtType.Double:
                    builder.Append(GetDouble()).Append('d');
                    break;

                case NbtType.Compound:
                    builder.Append(tagType.ToString());
                    builder.Append('{').Append(GetLength()).Append('}');
                    break;

                case NbtType.List:
                    var listType = GetBaseType();
                    builder.Append('<').Append(listType).Append('>');
                    builder.Append('[').Append(GetLength()).Append(']');
                    break;

                case NbtType.ByteArray:
                case NbtType.IntArray:
                case NbtType.LongArray:
                    var arrayType = GetBaseType();
                    builder.Append(arrayType);
                    builder.Append('[').Append(GetLength()).Append(']');
                    break;

                default:
                    builder.Append(tagType.ToString());
                    break;
            }

            return builder.ToString();
        }
    }
}
