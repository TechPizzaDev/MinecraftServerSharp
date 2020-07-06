using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Unicode;
using System.Threading;

namespace MinecraftServerSharp.NBT
{
    /// <summary>
    /// Provides a mechanism for examining the structural content 
    /// of an NBT value without automatically instantiating data values.
    /// </summary>
    public sealed partial class NbtDocument : IDisposable
    {
        private ReadOnlyMemory<byte> _data;
        private MetadataDb _metaDb;
        private byte[]? _extraRentedBytes;
        private (int, string?) _lastIndexAndString = (-1, null);

        internal bool IsDisposable { get; }

        /// <summary>
        /// The <see cref="NbtElement"/> representing the value of the document.
        /// </summary>
        public NbtElement RootTag => new NbtElement(this, 0);

        private NbtDocument(
            ReadOnlyMemory<byte> data,
            MetadataDb parsedData,
            byte[]? extraRentedBytes,
            bool isDisposable = true)
        {
            Debug.Assert(!data.IsEmpty);

            _data = data;
            _metaDb = parsedData;
            _extraRentedBytes = extraRentedBytes;

            IsDisposable = isDisposable;

            // extraRentedBytes better be null if we're not disposable.
            Debug.Assert(isDisposable || extraRentedBytes == null);
        }

        public void Dispose()
        {
            int length = _data.Length;
            if (length == 0 || !IsDisposable)
                return;

            _metaDb.Dispose();
            _data = ReadOnlyMemory<byte>.Empty;

            // When "extra rented bytes exist" they contain the document,
            // and thus need to be cleared before being returned.
            byte[]? extraRentedBytes = Interlocked.Exchange(ref _extraRentedBytes, null);
            if (extraRentedBytes != null)
            {
                extraRentedBytes.AsSpan(0, length).Clear();
                ArrayPool<byte>.Shared.Return(extraRentedBytes);
            }
        }

        internal NbtType GetTagType(int index)
        {
            CheckNotDisposed();
            return _metaDb.GetTagType(index);
        }

        internal int GetContainerLength(int index)
        {
            CheckNotDisposed();
            return _metaDb.GetLength(index);
        }

        internal NbtElement GetContainerElement(int index, int containerIndex)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            if (!row.TagType.IsContainer())
                throw new Exception("The tag is not a container.");

            int length = row.Length;
            if ((uint)containerIndex >= (uint)length)
                throw new IndexOutOfRangeException();

            int elementCount = 0;
            int objectOffset = index + DbRow.Size;

            for (; objectOffset < _metaDb.Length; objectOffset += DbRow.Size)
            {
                if (containerIndex == elementCount)
                    return new NbtElement(this, objectOffset);

                row = _metaDb.GetRow(objectOffset);

                if (row.IsContainerType)
                    objectOffset += DbRow.Size * row.Length;

                elementCount++;
            }

            Debug.Fail(
                $"Ran out of database searching for array index " +
                $"{containerIndex} from {index} when length was {length}");

            throw new IndexOutOfRangeException();
        }

        internal static int GetEndTagIndex(int baseIndex, in DbRow row)
        {
            if (row.IsPrimitiveType)
                return baseIndex + DbRow.Size;

            int endIndex = baseIndex + row.Length * DbRow.Size;
            return endIndex;
        }

        internal int GetEndIndex(int index)
        {
            CheckNotDisposed();
            DbRow row = _metaDb.GetRow(index);

            return GetEndTagIndex(index, row);
        }

        private ReadOnlyMemory<byte> GetRawData(int index, bool includeEndTag)
        {
            CheckNotDisposed();
            DbRow row = _metaDb.GetRow(index);

            if (row.IsPrimitiveType)
                return _data.Slice(row.Location, row.Length);

            int endTagIndex = GetEndTagIndex(index, row);
            int start = row.Location;
            int end = _metaDb.GetLocation(endTagIndex);
            return _data[start..end];
        }

        internal string? GetString(int index)
        {
            CheckNotDisposed();

            (int lastIndex, string? lastString) = _lastIndexAndString;
            if (lastIndex == index)
            {
                Debug.Assert(lastString != null);
                return lastString;
            }

            DbRow row = _metaDb.GetRow(index);
            CheckExpectedType(NbtType.String, row.TagType);

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location + sizeof(ushort), row.Length);
            lastString = Encoding.UTF8.GetString(segment);
            Debug.Assert(lastString != null);

            _lastIndexAndString = (index, lastString);
            return lastString;
        }

        internal bool StringEquals(int index, ReadOnlySpan<char> otherText)
        {
            throw new NotImplementedException();

            //CheckNotDisposed();
            //
            //(int lastIndex, string? lastString) = _lastIndexAndString;
            //if (lastIndex == index)
            //    return otherText.SequenceEqual(lastString);
            //
            //OperationStatus status = Utf8.FromUtf16(otherText, otherUtf8Text, out int consumed, out int written);
            //Debug.Assert(status != OperationStatus.DestinationTooSmall);
            //bool result;
            //if (status == OperationStatus.NeedMoreData || status == OperationStatus.InvalidData)
            //{
            //    result = false;
            //}
            //else
            //{
            //    Debug.Assert(status == OperationStatus.Done);
            //    Debug.Assert(consumed == utf16Text.Length);
            //
            //    result = TextEquals(index, otherUtf8Text.Slice(0, written), isTagName);
            //}
            //
            //if (otherUtf8TextArray != null)
            //{
            //    otherUtf8Text.Slice(0, written).Clear();
            //    ArrayPool<byte>.Shared.Return(otherUtf8TextArray);
            //}
            //
            //return result;
        }

        internal bool StringOrByteArrayEquals(int index, ReadOnlySpan<byte> otherData)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            var segment = type switch
            {
                NbtType.ByteArray => _data.Span.Slice(row.Location + sizeof(int), row.Length * sizeof(sbyte)),
                NbtType.String => _data.Span.Slice(row.Location + sizeof(short), row.Length * sizeof(byte)),
                _ => throw GetWrongTagTypeException(type),
            };
            return segment.SequenceEqual(otherData);
        }

        internal ReadOnlyMemory<byte> GetArray(int index)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            var segment = type switch
            {
                NbtType.String => _data.Slice(row.Location + sizeof(ushort), row.Length * sizeof(byte)),
                NbtType.ByteArray => _data.Slice(row.Location + sizeof(int), row.Length * sizeof(sbyte)),
                NbtType.IntArray => _data.Slice(row.Location + sizeof(int), row.Length * sizeof(int)),
                NbtType.LongArray => _data.Slice(row.Location + sizeof(int), row.Length * sizeof(long)),
                _ => throw GetWrongTagTypeException(type),
            };
            return segment;
        }

        internal sbyte GetSByte(int index)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            CheckExpectedType(NbtType.Byte, row.TagType);

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);
            return ReadSByte(segment);
        }

        internal short GetShort(int index, bool isBigEndian)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);
            return type switch
            {
                NbtType.Short => ReadShort(segment, isBigEndian),
                NbtType.Byte => ReadSByte(segment),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal int GetInt(int index, bool isBigEndian)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);
            return type switch
            {
                NbtType.Int => ReadInt(segment, isBigEndian),
                NbtType.Short => ReadShort(segment, isBigEndian),
                NbtType.Byte => ReadSByte(segment),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal float GetFloat(int index, bool isBigEndian)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);
            return type switch
            {
                NbtType.Float => ReadFloat(segment, isBigEndian),
                NbtType.Double => (float)ReadDouble(segment, isBigEndian),
                NbtType.Long => ReadLong(segment, isBigEndian),
                NbtType.Int => ReadInt(segment, isBigEndian),
                NbtType.Short => ReadShort(segment, isBigEndian),
                NbtType.Byte => ReadSByte(segment),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal double GetDouble(int index, bool isBigEndian)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);
            var type = row.TagType;

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);
            return type switch
            {
                NbtType.Double => ReadDouble(segment, isBigEndian),
                NbtType.Float => ReadFloat(segment, isBigEndian),
                NbtType.Long => ReadLong(segment, isBigEndian),
                NbtType.Int => ReadInt(segment, isBigEndian),
                NbtType.Short => ReadShort(segment, isBigEndian),
                NbtType.Byte => ReadSByte(segment),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal NbtElement CloneTag(int index)
        {
            int endIndex = GetEndIndex(index);
            MetadataDb newDb = _metaDb.CopySegment(index, endIndex);

            var segment = GetRawData(index, includeEndTag: true);
            byte[] segmentCopy = ArrayPool<byte>.Shared.Rent(segment.Length);
            segment.CopyTo(segmentCopy);

            var newDocument =
                new NbtDocument(segmentCopy, newDb, extraRentedBytes: segmentCopy);

            return newDocument.RootTag;
        }

        internal void WriteTagTo(int index, NbtWriter writer)
        {
            CheckNotDisposed();

            DbRow row = _metaDb.GetRow(index);

            throw new NotImplementedException();

            switch (row.TagType)
            {
                case NbtType.Compound:
                    writer.WriteCompoundStart(row.Length);
                    return;

                case NbtType.List:
                    throw new NotImplementedException();
                    //writer.WriteListStart(row.Length, );
                    return;

                case NbtType.Null:
                    throw new Exception($"Unexpected {row.TagType} tag.");

                default:
                    return;
            }
        }

        private void WriteContainer(int index, NbtWriter writer)
        {
            int endIndex = GetEndIndex(index);

            ReadOnlySpan<byte> data = _data.Span;
            for (int i = index + DbRow.Size; i < endIndex; i += DbRow.Size)
            {
                DbRow row = _metaDb.GetRow(i);
                throw new NotImplementedException();
                //ReadOnlySpan<byte> segment = data.Slice(row.Location, gib length here);
                //writer.WriteRaw(segment);
            }
        }

        private static void ClearAndReturn(ArraySegment<byte> rented)
        {
            if (rented.Array != null)
            {
                rented.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(rented.Array);
            }
        }

        #region Read Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte ReadSByte(ReadOnlySpan<byte> source)
        {
            return MemoryMarshal.Read<sbyte>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short ReadShort(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadInt16LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadUShort(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadUInt16BigEndian(source);
            else
                return BinaryPrimitives.ReadUInt16LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadInt(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt32BigEndian(source);
            else
                return BinaryPrimitives.ReadInt32LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadLong(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            if (isBigEndian)
                return BinaryPrimitives.ReadInt64BigEndian(source);
            else
                return BinaryPrimitives.ReadInt64LittleEndian(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ReadFloat(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            int intValue = ReadInt(source, isBigEndian);
            return BitConverter.Int32BitsToSingle(intValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadDouble(ReadOnlySpan<byte> source, bool isBigEndian)
        {
            long intValue = ReadLong(source, isBigEndian);
            return BitConverter.Int64BitsToDouble(intValue);
        }

        #endregion

        private static void Parse(
            ReadOnlySpan<byte> data,
            NbtReaderOptions readerOptions,
            ref MetadataDb database,
            ref RowFrameStack stack)
        {
            var reader = new NbtReader(
                data,
                isFinalBlock: true,
                new NbtReaderState(readerOptions));

            var f = new RowFrame();

            while (reader.Read())
            {
                NbtType tagType = reader.TagType;

                // Since the input payload is contained within a Span,
                // token start index can never be larger than int.MaxValue (i.e. data.Length).
                Debug.Assert(reader.TagStartIndex <= int.MaxValue);
                int tagStart = (int)reader.TagStartIndex;

                Debug.Assert(tagType >= NbtType.End && tagType < NbtType.Null);

                if (tagType == NbtType.Compound)
                {
                    int length = ReadInt(reader.ValueSpan, readerOptions.IsBigEndian);

                    database.Append(tagStart, length, numberOfRows: 0, tagType, !f.SkipName);

                    stack.Push(f);
                    f = default;
                }
                else if (tagType == NbtType.End)
                {

                }
                else if (tagType == NbtType.List)
                {
                    int length = ReadInt(reader.ValueSpan, readerOptions.IsBigEndian);
                    f.ListTagsLeft = length;

                    stack.Push(f);
                    f = default;
                    f.SkipName = true;
                }
                else
                {
                    if (f.ListTagsLeft > 0)
                    {
                        f.ListTagsLeft--;

                        if (f.ListTagsLeft == 0)
                            f = stack.Pop();
                    }
                    else
                    {
                        f.NumberOfRows++;
                    }

                    database.Append(tagStart, reader.ValueSpan.Length, numberOfRows: 1, tagType, !f.SkipName);
                }
            }

            Debug.Assert(reader.BytesConsumed == data.Length);
            database.TrimExcess();
        }

        private void CheckNotDisposed()
        {
            if (_data.IsEmpty)
                throw new ObjectDisposedException(nameof(NbtDocument));
        }

        private void CheckExpectedType(NbtType expected, NbtType actual)
        {
            if (expected != actual)
                throw GetWrongTagTypeException(actual);
        }

        private static InvalidOperationException GetWrongTagTypeException(NbtType type)
        {
            throw new InvalidOperationException($"Unexpected tag type \"{type}\".");
        }

        private static void CheckSupportedOptions(NbtReaderOptions readerOptions, string paramName)
        {
        }
    }
}
