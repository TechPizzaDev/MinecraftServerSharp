﻿using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MCServerSharp.Utility;

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
        /// Gets the raw name of the last processed element as a slice of the input.
        /// </summary>
        public ReadOnlySpan<byte> NameSpan { get; private set; }

        /// <summary>
        /// Gets the raw value of the last processed element as a slice of the input.
        /// </summary>
        public ReadOnlySpan<byte> ValueSpan { get; private set; }

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
        public NbtReaderState CurrentState => _state;

        public NbtOptions Options => _state.Options;

        /// <summary>
        /// Gets the total number of bytes consumed so far by this instance.
        /// </summary>
        public long BytesConsumed => _consumed;

        /// <summary>
        /// Gets the depth of the current element.
        /// </summary>
        public int CurrentDepth
        {
            get
            {
                int depth = _state._containerInfoStack.Count;
                if (TagType.IsContainer())
                    depth--;
                return depth;
            }
        }

        public Utf8String NameString => new Utf8String(NameSpan);

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

        // Summary:
        //     Initializes a new instance of the System.Text.Json.Utf8JsonReader structure that
        //     processes a read-only span of UTF-8 encoded text using the specified options.
        //
        // Parameters:
        //   jsonData:
        //     The UTF-8 encoded JSON text to process.
        //
        //   options:
        //     Defines customized behavior of the System.Text.Json.Utf8JsonReader that differs
        //     from the JSON RFC (for example how to handle comments or maximum depth allowed
        //     when reading). By default, the System.Text.Json.Utf8JsonReader follows the JSON
        //     RFC strictly; comments within the JSON are invalid, and the maximum depth is
        //     64.
        public NbtReader(ReadOnlySpan<byte> data, NbtOptions? options = default) :
            this(data, true, new NbtReaderState(options))
        {
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMoreData()
        {
            if (EndOfDocument)
                return false;

            if (_consumed >= _data.Length)
            {
                //if (_isNotPrimitive && IsLastSpan)
                //{
                //    if (_bitStack.CurrentDepth != 0)
                //    {
                //        ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ZeroDepthAtEnd);
                //    }
                //
                //    if (_tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
                //    {
                //        ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive);
                //    }
                //}
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads the next NBT element from the input source.
        /// </summary>
        /// <returns>true if the element was read successfully; otherwise, false.</returns>
        /// <exception cref="NbtDepthException">
        /// The current depth exceeds <see cref="NbtOptions.MaxDepth"/>.
        /// </exception>
        /// <exception cref="NbtReadException">
        /// An invalid NBT element is encountered.
        /// </exception>
        public bool Read()
        {
            TagSpan = default;
            NameSpan = default;
            ValueSpan = default;
            TagFlags = default;
            TagCollectionLength = default;

            if (!HasMoreData())
                return false;

            // TODO: respect max depth setting and IsFinalBlock

            // TODO: check if last tag was fully read

            var slice = _data[_consumed..];
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
                else if(frame.ListEntriesRemaining != -1)
                {
                    inList = true;
                    frame.ListEntriesRemaining--;
                }
            }

            TagFlags |= Options.IsBigEndian ? NbtFlags.BigEndian : NbtFlags.LittleEndian;

            if (inList)
            {
                TagType = frame.ElementType;
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

                    int nameLength = ReadStringLength(slice[read..], Options, out int nameLengthBytes);
                    read += nameLengthBytes;

                    NameSpan = slice.Slice(read, nameLength);
                    read += nameLength;
                }
            }

            switch (TagType)
            {
                case NbtType.Compound:
                    _state._containerInfoStack.Push(new ContainerFrame
                    {
                        ListEntriesRemaining = -1
                    });
                    break;

                case NbtType.End:
                    // Documents with a single End tag are valid.
                    _state._containerInfoStack.TryPop();

                    if (_state._containerInfoStack.ByteCount == 0)
                        EndOfDocument = true;
                    break;

                case NbtType.List:
                {
                    var listType = (NbtType)slice[read];

                    TagCollectionLength = ReadListLength(
                        slice[(sizeof(byte) + read)..], Options, out int listLengthBytes);

                    if (TagCollectionLength <= 0 && listType != NbtType.End)
                    {
                        throw new NbtReadException(
                            $"{NbtType.List} length was less than or equal to zero " +
                            $"but the type was not {NbtType.End}.");
                    }

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
                    TagCollectionLength = ReadArrayLength(slice[read..], Options, out int arrayLengthBytes);
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(sbyte));
                    break;
                }

                case NbtType.IntArray:
                {
                    TagCollectionLength = ReadArrayLength(slice[read..], Options, out int arrayLengthBytes);
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(int));
                    break;
                }

                case NbtType.LongArray:
                {
                    TagCollectionLength = ReadArrayLength(slice[read..], Options, out int arrayLengthBytes);
                    read += arrayLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength * sizeof(long));
                    break;
                }

                case NbtType.String:
                    TagCollectionLength = ReadStringLength(slice[read..], Options, out int stringLengthBytes);
                    read += stringLengthBytes;

                    ValueSpan = slice.Slice(read, TagCollectionLength);
                    break;

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
                    throw new InvalidDataException($"Unexpected tag type \"{TagType}\".");
            }
            read += ValueSpan.Length;

            TagLocation = _consumed;
            TagSpan = slice.Slice(0, read);
            _consumed += read;
            return true;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<byte> SkipTagName(
            ReadOnlyMemory<byte> source, NbtOptions options)
        {
            int nameLength = ReadStringLength(source.Span, options, out int lengthBytes);
            return source[(lengthBytes + nameLength)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<byte> SkipTagType(ReadOnlyMemory<byte> source)
        {
            return source[sizeof(byte)..];
        }

        public static int ReadStringLength(
            ReadOnlySpan<byte> source, NbtOptions options, out int bytesConsumed)
        {
            int result;
            if (options.IsVarInt)
            {
                var status = VarInt.TryDecode(source, out var value, out bytesConsumed);
                if (status != System.Buffers.OperationStatus.Done)
                    throw new Exception(status.ToString());
                result = value;
            }
            else
            {
                result = ReadShort(source, options.IsBigEndian);
                bytesConsumed = sizeof(short);

                if (result > short.MaxValue)
                    throw new Exception("Name length exceeds maximum.");
            }

            if (result < 0)
                throw new Exception("Negative tag name length.");
            return result;
        }

        public static int ReadArrayLength(
            ReadOnlySpan<byte> source, NbtOptions options, out int bytesConsumed)
        {
            if (options.IsVarInt)
            {
                var status = VarInt.TryDecode(source, out var value, out bytesConsumed);
                if (status != System.Buffers.OperationStatus.Done)
                    throw new Exception(status.ToString());
                return value;
            }
            else
            {
                int result = ReadInt(source, options.IsBigEndian);
                bytesConsumed = sizeof(int);
                return result;
            }
        }

        public static int ReadListLength(
            ReadOnlySpan<byte> source, NbtOptions options, out int bytesConsumed)
        {
            return ReadArrayLength(source, options, out bytesConsumed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadByte(ReadOnlySpan<byte> source)
        {
            return MemoryMarshal.Read<sbyte>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadInt16LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadUInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadUInt16LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt32BigEndian(source);
            else
                return BinaryPrimitives.ReadInt32LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt64BigEndian(source);
            else
                return BinaryPrimitives.ReadInt64LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            int intValue = ReadInt(source, isBigEndian);
            return BitConverter.Int32BitsToSingle(intValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            long intValue = ReadLong(source, isBigEndian);
            return BitConverter.Int64BitsToDouble(intValue);
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        internal struct ContainerFrame
        {
            public int ListEntriesRemaining;
            public NbtType ElementType;
        }
    }
}
