using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MCServerSharp.NBT
{
    // TODO: ArrayEnumerator<T>

    [DebuggerDisplay("{ToString(), nq}")]
    public readonly partial struct NbtElement
    {
        private readonly NbtDocument _parent;
        private readonly int _index;

        /// <summary>
        /// Gets an element within this container.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">This element is not a container.</exception>
        public NbtElement this[int index]
        {
            get
            {
                CheckValidInstance();
                return _parent.GetContainerElement(_index, index);
            }
        }

        public NbtType Type => _parent?.GetTagType(_index) ?? NbtType.Undefined;

        public NbtFlags Flags => _parent?.GetFlags(_index) ?? NbtFlags.None;

        public ReadOnlySpan<byte> Name => _parent != null ? _parent.GetTagName(_index) : ReadOnlySpan<byte>.Empty;

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
            CheckValidInstance();

            var type = Type;
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
        /// Gets the number of elements contained within the current array-like element.
        /// </summary>
        /// <returns>The number of elements contained within the current element.</returns>
        /// <exception cref="InvalidOperationException">This element is not an array-like.</exception>
        /// <exception cref="ObjectDisposedException">The parent <see cref="NbtDocument"/> has been disposed.</exception>
        public int GetLength()
        {
            CheckValidInstance();
            return _parent.GetLength(_index);
        }

        public sbyte GetByte()
        {
            CheckValidInstance();
            return _parent.GetByte(_index);
        }

        public short GetShort()
        {
            CheckValidInstance();
            return _parent.GetShort(_index);
        }

        public int GetInt()
        {
            CheckValidInstance();
            return _parent.GetInt(_index);
        }

        public long GetLong()
        {
            CheckValidInstance();
            return _parent.GetLong(_index);
        }

        public float GetFloat()
        {
            CheckValidInstance();
            return _parent.GetFloat(_index);
        }

        public double GetDouble()
        {
            CheckValidInstance();
            return _parent.GetDouble(_index);
        }

        public string GetString()
        {
            CheckValidInstance();
            return _parent.GetString(_index);
        }

        public ContainerEnumerator EnumerateContainer()
        {
            CheckValidInstance();

            if (!Type.IsContainer())
                throw new InvalidOperationException("This element is not a container.");

            return new ContainerEnumerator(this);
        }

        public ArrayEnumerator<T> EnumerateArray<T>()
            where T : unmanaged
        {
            CheckValidInstance();

            if (!Type.IsArray())
                throw new InvalidOperationException("This element is not an array.");

            return new ArrayEnumerator<T>(this);
        }

        public void WriteTo(NbtWriter writer)
        {
            CheckValidInstance();

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
        ///     If this NbtElement is itself the output of a previous call to Clone, or
        ///     a value contained within another NbtElement which was the output of a previous
        ///     call to Clone, this method results in no additional memory allocation.
        ///   </para>
        /// </remarks>
        public NbtElement Clone()
        {
            CheckValidInstance();

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
            CheckValidInstance();

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
            CheckValidInstance();

            return _parent.StringEquals(_index, text);
        }

        private void CheckValidInstance()
        {
            if (_parent == null)
                throw new InvalidOperationException();
        }

        public override string ToString()
        {
            var name = Name.ToUtf8String();
            var builder = new StringBuilder(name.Length + 10);

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
