using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.NBT
{
    public ref struct NbtReader
    {
        private ReadOnlySpan<byte> _data;
        private NbtReaderState _state;

        private int _consumed;

        /// <summary>
        /// Gets the mode of this instance of the <see cref="NbtReader"/> which indicates
        /// whether all the data was provided or there is more data to come.
        /// </summary>
        /// <returns>
        /// true if the reader was constructed with the input span containing
        /// the entire data to process; false if the reader was constructed with an
        /// input span that may contain partial data with more data to follow.
        /// </returns>
        public bool IsFinalBlock { get; }

        /// <summary>
        /// Gets whether the reader has emptied it's state stack and finished reading a document.
        /// </summary>
        public bool EndOfDocument { get; private set; }

        /// <summary>
        /// Gets the index that the last processed element starts at (within the given input data).
        /// </summary>
        public int TagLocation { get; private set; }

        /// <summary>
        /// Gets the type of the last processed element.
        /// </summary>
        public NbtType TagType { get; private set; }

        /// <summary>
        /// Gets the flags of the last processed element.
        /// </summary>
        public NbtFlags TagFlags { get; private set; }

        /// <summary>
        /// Gets the raw data of the last processed element as a slice of the input.
        /// </summary>
        public ReadOnlySpan<byte> TagSpan { get; private set; }

        /// <summary>
        /// Gets the raw name data of the last processed element as a slice of the input.
        /// </summary>
        public ReadOnlySpan<byte> RawNameSpan { get; private set; }

        /// <summary>
        /// Gets the raw value data of the last processed element as a slice of the input.
        /// </summary>
        public ReadOnlySpan<byte> ValueSpan { get; private set; }

        /// <summary>
        /// Gets the amount of bytes at the head of <see cref="RawNameSpan"/> that
        /// defines the <see cref="NameSpan"/> length.
        /// </summary>
        public int NameLengthBytes { get; private set; }

        /// <summary>
        /// Gets the prefixed length of an array type.
        /// </summary>
        /// <remarks>
        /// <see cref="NbtType.Compound"/> does not provide a length.
        /// <see cref="NbtType.List"/> may have negative length when it's of <see cref="NbtType.End"/>.
        /// </remarks>
        public int TagCollectionLength { get; private set; }

        /// <summary>
        /// Gets the current <see cref="NbtReader"/> state to pass to a <see cref="NbtReader"/>
        /// constructor with more data.
        /// </summary>
        public readonly NbtReaderState CurrentState => _state;

        public readonly NbtOptions Options => _state.Options;

        /// <summary>
        /// Gets the total number of bytes consumed so far by this instance.
        /// </summary>
        public readonly long BytesConsumed => _consumed;

        /// <summary>
        /// Gets the depth of the current element.
        /// </summary>
        public readonly int CurrentDepth
        {
            get
            {
                int depth = _state._containerInfoStack.Count;
                if (TagType.IsContainer())
                    depth--;
                return depth;
            }
        }

        public readonly int NameLength
        {
            get
            {
                TryReadStringLength(RawNameSpan, Options, out _, out int length);
                return length;
            }
        }

        public readonly ReadOnlySpan<byte> NameSpan => RawNameSpan.Slice(NameLengthBytes, NameLength);

        public readonly Utf8String NameString => new(NameSpan);

        #region Cosntructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NbtReader"/> structure that
        /// processes a read-only span of binary data and indicates whether the input
        /// contains all the data to process.
        /// </summary>
        /// <param name="data">
        /// The binary data to process.
        /// </param>
        /// <param name="isFinalBlock">
        /// true to indicate that the input sequence contains the entire data to process;
        /// false to indicate that the input span contains partial data with more data to follow.
        /// </param>
        /// <param name="state">
        /// An object that contains the reader state. If this is the first call to the constructor,
        /// pass the default state; otherwise, pass the value of the <see cref="CurrentState"/>
        /// property from the previous instance of the <see cref="NbtReader"/>.
        /// </param>
        public NbtReader(ReadOnlySpan<byte> data, bool isFinalBlock, NbtReaderState state) : this()
        {
            _data = data;
            IsFinalBlock = isFinalBlock;
            _state = state;
            TagType = NbtType.Undefined;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMoreData()
        {
            if (EndOfDocument)
                return false;

            return _consumed < _data.Length;
        }

        /// <summary>
        /// Reads the next NBT element from the input source.
        /// </summary>
        /// <returns>true if the element was read successfully; otherwise, false.</returns>
        /// <exception cref="NbtDepthException">
        /// The current depth exceeds <see cref="NbtOptions.MaxDepth"/>.
        /// </exception>
        public NbtReadStatus TryRead()
        {
            NameLengthBytes = 0;
            RawNameSpan = default;
            TagCollectionLength = default;

            NbtReadStatus status;

            if (!HasMoreData())
            {
                status = NbtReadStatus.EndOfDocument;
                goto ClearReturn;
            }

            // TODO: respect max depth setting and IsFinalBlock

            // TODO: check if last tag was fully read

            ReadOnlySpan<byte> slice = _data[_consumed..];
            int read = 0;

            PeekStack:
            bool inList = false;
            ref ContainerFrame frame = ref _state._containerInfoStack.TryPeek();
            if (!Unsafe.IsNullRef(ref frame))
            {
                if (frame.ListEntriesRemaining == 0)
                {
                    _state._containerInfoStack.TryPop();
                    goto PeekStack;
                }
                else if (frame.ListEntriesRemaining != -1)
                {
                    inList = true;
                    frame.ListEntriesRemaining--;
                }
            }

            if (inList)
            {
                TagType = frame.ElementType;
                TagFlags = NbtFlags.None;
            }
            else
            {
                TagType = (NbtType)slice[read++];
                TagFlags |= NbtFlags.Typed;

                // Tags in lists don't have a name.
                // End tags don't have a name ever.
                if (TagType != NbtType.End)
                {
                    TagFlags |= NbtFlags.Named;

                    status = TryReadStringLength(
                        slice[read..],
                        Options,
                        out int nameLengthBytes,
                        out int nameLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;
                    if (nameLength < 0)
                    {
                        status = NbtReadStatus.InvalidNameLength;
                        goto ClearReturn;
                    }
                     
                    int rawNameLength = nameLengthBytes + nameLength;

                    NameLengthBytes = nameLengthBytes;
                    RawNameSpan = slice.Slice(read, rawNameLength);
                    read += rawNameLength;
                }
            }

            switch (TagType)
            {
                case NbtType.Compound:
                    _state._containerInfoStack.Push(new ContainerFrame
                    {
                        ListEntriesRemaining = -1
                    });

                    ValueSpan = default;
                    break;

                case NbtType.End:
                    // Documents with a single End tag are valid.
                    _state._containerInfoStack.TryPop();

                    if (_state._containerInfoStack.ByteCount == 0)
                        EndOfDocument = true;

                    ValueSpan = default;
                    break;

                case NbtType.List:
                {
                    var listType = (NbtType)slice[read];

                    status = TryReadListLength(
                        slice[(sizeof(byte) + read)..],
                        Options,
                        out int listLengthBytes,
                        out int listLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;
                    if (listLength <= 0 && listType != NbtType.End)
                    {
                        status = NbtReadStatus.InvalidListLength;
                        goto ClearReturn;
                    }

                    TagCollectionLength = listLength;

                    _state._containerInfoStack.Push(new ContainerFrame
                    {
                        ElementType = listType,

                        // Clamp length in case of negative length
                        ListEntriesRemaining = Math.Max(TagCollectionLength, 0)
                    });

                    ValueSpan = slice.Slice(read, sizeof(byte) + listLengthBytes);
                    break;
                }

                case NbtType.ByteArray:
                {
                    status = TryReadArrayLength(
                        slice[read..],
                        Options,
                        out int arrayLengthBytes,
                        out int arrayLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;

                    TagCollectionLength = arrayLength;
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(sbyte));
                    break;
                }

                case NbtType.IntArray:
                {
                    status = TryReadArrayLength(
                        slice[read..],
                        Options,
                        out int arrayLengthBytes,
                        out int arrayLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;

                    TagCollectionLength = arrayLength;
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(int));
                    break;
                }

                case NbtType.LongArray:
                {
                    status = TryReadArrayLength(
                        slice[read..],
                        Options,
                        out int arrayLengthBytes,
                        out int arrayLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;

                    TagCollectionLength = arrayLength;
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(long));
                    break;
                }

                case NbtType.String:
                {
                    status = TryReadStringLength(
                        slice[read..],
                        Options,
                        out int stringLengthBytes,
                        out int stringLength);

                    if (status != NbtReadStatus.Done)
                        goto ClearReturn;

                    TagCollectionLength = stringLength;
                    read += stringLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength);
                    break;
                }

                case NbtType.Byte:
                    ValueSpan = slice.Slice(read, sizeof(sbyte));
                    break;

                case NbtType.Int:
                    ValueSpan = slice.Slice(read, sizeof(int));
                    break;

                case NbtType.Short:
                    ValueSpan = slice.Slice(read, sizeof(short));
                    break;

                case NbtType.Long:
                    ValueSpan = slice.Slice(read, sizeof(long));
                    break;

                case NbtType.Float:
                    ValueSpan = slice.Slice(read, sizeof(float));
                    break;

                case NbtType.Double:
                    ValueSpan = slice.Slice(read, sizeof(double));
                    break;

                default:
                    status = NbtReadStatus.UnknownTag;
                    goto ClearReturn;
            }
            read += ValueSpan.Length;

            TagLocation = _consumed;
            TagSpan = slice.Slice(0, read);
            _consumed += read;
            return NbtReadStatus.Done;

            ClearReturn:
            TagSpan = default;
            ValueSpan = default;
            TagFlags = default;
            RawNameSpan = default;
            TagCollectionLength = default;
            return status;
        }

        // Summary:
        //     Skips the children of the current token.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The reader was given partial data with more data to follow
        //     (that is, System.Text.Json.Utf8JsonReader.IsFinalBlock is false).
        //
        //   T:System.Text.Json.JsonException:
        //     An invalid JSON token was encountered while skipping, according to the JSON RFC.
        //     -or- The current depth exceeds the recursive limit set by the maximum depth.
        public void Skip()
        {
            // remember IsFinalBlock
            throw new NotImplementedException();
        }

        public bool TrySkip()
        {
            throw new NotImplementedException();
        }

        public sbyte GetSByte()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public short GetInt16()
        {
            throw new NotImplementedException();
        }

        public int GetInt32()
        {
            throw new NotImplementedException();
        }

        public long GetInt64()
        {
            throw new NotImplementedException();
        }

        public float GetSingle()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetInt16(out short value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetInt32(out int value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetInt64(out long value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSByte(out sbyte value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSingle(out float value)
        {
            throw new NotImplementedException();
        }

        public bool ValueSequenceEqual(ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public bool ValueStringEquals(ReadOnlySpan<char> text)
        {
            throw new NotImplementedException();
        }

        public bool ValueStringEquals(string? text)
        {
            return ValueStringEquals(text.AsSpan());
        }

        #region Read Helpers

        internal static NbtReadStatus TrySkipTagName(
            ReadOnlyMemory<byte> source, in NbtOptions options, out ReadOnlyMemory<byte> result)
        {
            var status = TryReadStringLength(
                source.Span, options, out int lengthBytes, out int nameLength);

            if (status == NbtReadStatus.Done)
            {
                result = source[(lengthBytes + nameLength)..];
                return NbtReadStatus.Done;
            }
            result = default;
            return status;
        }


        internal static NbtReadStatus TrySkipTagName(
            ReadOnlySpan<byte> source, in NbtOptions options, out ReadOnlySpan<byte> result)
        {
            var status = TryReadStringLength(
                source, options, out int lengthBytes, out int nameLength);

            if (status == NbtReadStatus.Done)
            {
                result = source[(lengthBytes + nameLength)..];
                return NbtReadStatus.Done;
            }
            result = default;
            return status;
        }

        internal static NbtReadStatus TrySkipTagType(
            ReadOnlyMemory<byte> source, out ReadOnlyMemory<byte> result)
        {
            if (source.IsEmpty)
            {
                result = default;
                return NbtReadStatus.NeedMoreData;
            }
            result = source[sizeof(byte)..];
            return NbtReadStatus.Done;
        }

        internal static NbtReadStatus TrySkipTagType(
            ReadOnlySpan<byte> source, out ReadOnlySpan<byte> result)
        {
            if (source.IsEmpty)
            {
                result = default;
                return NbtReadStatus.NeedMoreData;
            }
            result = source[sizeof(byte)..];
            return NbtReadStatus.Done;
        }

        /// <summary>
        /// Reads a string length without extra validation for invalid data.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="options"></param>
        /// <param name="bytesConsumed"></param>
        /// <returns>
        /// The length of the string.
        /// May be less than zero which indicates unexpected data.
        /// </returns>
        public static NbtReadStatus TryReadStringLength(
            ReadOnlySpan<byte> source, in NbtOptions options,
            out int bytesConsumed, out int value)
        {
            if (options.IsVarStringLength)
            {
                var status = VarInt.TryDecode(source, out VarInt decoded, out bytesConsumed);
                if (status != OperationStatus.Done)
                {
                    value = default;
                    bytesConsumed = 0;
                    return (NbtReadStatus)status;
                }
                value = decoded;
            }
            else
            {
                if (!TryReadInt16(source, options.IsBigEndian, out short decoded))
                {
                    value = default;
                    bytesConsumed = 0;
                    return NbtReadStatus.NeedMoreData;
                }
                value = decoded;
                bytesConsumed = sizeof(short);
            }

            if (value < 0)
                return NbtReadStatus.InvalidStringLength;
            return NbtReadStatus.Done;
        }

        private static NbtReadStatus TryReadArrayOrListLength(
            ReadOnlySpan<byte> source, in NbtOptions options,
            out int bytesConsumed, out int value)
        {
            if (options.IsVarArrayLength)
            {
                var status = VarInt.TryDecode(source, out VarInt decoded, out bytesConsumed);
                if (status != OperationStatus.Done)
                {
                    value = default;
                    bytesConsumed = 0;
                    return (NbtReadStatus)status;
                }
                value = decoded;
            }
            else
            {
                if (!TryReadInt32(source, options.IsBigEndian, out int decoded))
                {
                    value = default;
                    bytesConsumed = 0;
                    return NbtReadStatus.NeedMoreData;
                }
                value = decoded;
                bytesConsumed = sizeof(int);
            }
            return NbtReadStatus.Done;
        }

        public static NbtReadStatus TryReadArrayLength(
            ReadOnlySpan<byte> source, in NbtOptions options,
            out int bytesConsumed, out int value)
        {
            var status = TryReadArrayOrListLength(
                source, options, out bytesConsumed, out value);

            if (status == NbtReadStatus.Done && value < 0)
                return NbtReadStatus.InvalidArrayLength;
            return status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NbtReadStatus TryReadListLength(
            ReadOnlySpan<byte> source, in NbtOptions options,
            out int bytesConsumed, out int value)
        {
            return TryReadArrayOrListLength(
                source, options, out bytesConsumed, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadByte(ReadOnlySpan<byte> source, out sbyte value)
        {
            return MemoryMarshal.TryRead(source, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadByte(ReadOnlySpan<byte> source)
        {
            return MemoryMarshal.Read<sbyte>(source);
        }

        public static bool TryReadInt16(ReadOnlySpan<byte> source, bool isBigEndian, out short value)
        {
            if (isBigEndian)
                return BinaryPrimitives.TryReadInt16BigEndian(source, out value);
            else
                return BinaryPrimitives.TryReadInt16LittleEndian(source, out value);
        }

        public static short ReadInt16(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadInt16LittleEndian(source);
        }

        public static bool TryReadUInt16(ReadOnlySpan<byte> source, bool isBigEndian, out ushort value)
        {
            if (isBigEndian)
                return BinaryPrimitives.TryReadUInt16BigEndian(source, out value);
            else
                return BinaryPrimitives.TryReadUInt16LittleEndian(source, out value);
        }

        public static ushort ReadUInt16(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadUInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadUInt16LittleEndian(source);
        }

        public static bool TryReadInt32(
            ReadOnlySpan<byte> source, bool isBigEndian, out int value)
        {
            if (isBigEndian)
                return BinaryPrimitives.TryReadInt32BigEndian(source, out value);
            else
                return BinaryPrimitives.TryReadInt32LittleEndian(source, out value);
        }

        public static int ReadInt32(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt32BigEndian(source);
            else
                return BinaryPrimitives.ReadInt32LittleEndian(source);
        }

        public static bool TryReadInt64(
            ReadOnlySpan<byte> source, bool isBigEndian, out long value)
        {
            if (isBigEndian)
                return BinaryPrimitives.TryReadInt64BigEndian(source, out value);
            else
                return BinaryPrimitives.TryReadInt64LittleEndian(source, out value);
        }

        public static long ReadInt64(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt64BigEndian(source);
            else
                return BinaryPrimitives.ReadInt64LittleEndian(source);
        }

        public static bool TryReadFloat(
            ReadOnlySpan<byte> source, bool isBigEndian, out float value)
        {
            if (TryReadInt32(source, isBigEndian, out int intValue))
            {
                value = BitConverter.Int32BitsToSingle(intValue);
                return true;
            }
            value = default;
            return false;
        }

        public static float ReadFloat(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            int intValue = ReadInt32(source, isBigEndian);
            float value = BitConverter.Int32BitsToSingle(intValue);
            return value;
        }

        public static bool TryReadDouble(
            ReadOnlySpan<byte> source, bool isBigEndian, out double value)
        {
            if (TryReadInt64(source, isBigEndian, out long intValue))
            {
                value = BitConverter.Int64BitsToDouble(intValue);
                return true;
            }
            value = default;
            return false;
        }

        public static double ReadDouble(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            long intValue = ReadInt64(source, isBigEndian);
            double value = BitConverter.Int64BitsToDouble(intValue);
            return value;
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        public struct ContainerFrame
        {
            public int ListEntriesRemaining;
            public NbtType ElementType;
        }
    }
}
