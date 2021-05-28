using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using static MCServerSharp.NBT.NbtReader;

namespace MCServerSharp.NBT
{
    /// <summary>
    /// Provides a mechanism for examining the structural content 
    /// of NBT without automatically instantiating data values.
    /// </summary>
    public sealed partial class NbtDocument : IDisposable
    {
        private MetadataDb _metaDb;
        private byte[]? _extraRentedBytes;
        private ArrayPool<byte>? _pool;
        private (int, string?) _lastIndexAndString = (-1, null);
        private (int, Utf8String?) _lastIndexAndUtf8String = (-1, null);

        internal bool IsDisposable => _pool != null;

        public NbtOptions Options { get; }

        public ReadOnlyMemory<byte> Bytes { get; private set; }

        public bool IsDisposed => Bytes.IsEmpty;

        /// <summary>
        /// The <see cref="NbtElement"/> representing the value of the document.
        /// </summary>
        public NbtElement RootTag => new(this, 0);

        private NbtDocument(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            MetadataDb parsedData,
            byte[]? extraRentedBytes,
            ArrayPool<byte>? pool)
        {
            Debug.Assert(!data.IsEmpty);

            Bytes = data;
            _metaDb = parsedData;
            _extraRentedBytes = extraRentedBytes;
            _pool = pool;
            Options = options;

            // extraRentedBytes better be null if we're not disposable.
            Debug.Assert(IsDisposable || extraRentedBytes == null);
        }

        public void Dispose()
        {
            int length = Bytes.Length;
            if (length == 0 || !IsDisposable)
                return;

            _metaDb.Dispose();
            Bytes = ReadOnlyMemory<byte>.Empty;

            // When "extra rented bytes exist" they contain the document,
            // and thus need to be cleared before being returned.
            byte[]? extraRentedBytes = Interlocked.Exchange(ref _extraRentedBytes, null);
            if (extraRentedBytes != null)
            {
                extraRentedBytes.AsSpan(0, length).Clear();
                _pool?.Return(extraRentedBytes);
            }
        }

        internal ref DbRow GetRow(int index)
        {
            // No need to check if document is disposed, database should check that.
            return ref _metaDb.GetRow(index);
        }

        internal NbtType GetTagType(int index)
        {
            return _metaDb.GetRow(index).Type;
        }

        internal int GetLength(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (!row.Type.IsCollection())
                throw new Exception("The tag is not a collection.");

            return row.CollectionLength;
        }

        internal NbtType GetListType(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (row.Type != NbtType.List)
                return NbtType.Undefined;

            TryGetTagPayloadSpan(row, out ReadOnlySpan<byte> payload);
            return (NbtType)ReadByte(payload);
        }

        internal NbtFlags GetFlags(int index)
        {
            return _metaDb.GetRow(index).Flags;
        }

        internal NbtElement GetContainerElement(int index, int arrayIndex)
        {
            ref readonly DbRow containerRow = ref _metaDb.GetRow(index);

            if (!containerRow.IsContainerType)
                throw new InvalidOperationException("The tag is not a container.");

            int length = containerRow.CollectionLength;
            if ((uint)arrayIndex >= (uint)length)
                throw new IndexOutOfRangeException();

            int elementCount = 0;
            int objectOffset = index + DbRow.Size;

            for (; objectOffset < _metaDb.ByteLength;)
            {
                if (arrayIndex == elementCount)
                    return new NbtElement(this, objectOffset);

                objectOffset += _metaDb.GetRow(objectOffset).RowCount * DbRow.Size;
                elementCount++;
            }

            Debug.Fail(
                $"Ran out of database searching for array index " +
                $"{arrayIndex} from {index} when length was {length}");

            throw new IndexOutOfRangeException();
        }

        internal static int GetEndIndex(int baseIndex, in DbRow row)
        {
            int endIndex = baseIndex + row.RowCount * DbRow.Size;
            return endIndex;
        }

        internal int GetEndIndex(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            int endIndex = GetEndIndex(index, row);
            return endIndex;
        }

        internal ReadOnlyMemory<byte> GetRawData(int index, out NbtType tagType)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.Type;
            int start = row.Location;
            int endIndex = GetEndIndex(index, row);
            if (endIndex == _metaDb.ByteLength)
                return Bytes[start..];

            int end = _metaDb.GetRow(endIndex).Location;
            return Bytes[start..end];
        }

        internal ReadOnlySpan<byte> GetRawDataSpan(int index, out NbtType tagType)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.Type;
            int start = row.Location;
            int endIndex = GetEndIndex(index, row);
            if (endIndex == _metaDb.ByteLength)
                return Bytes.Span[start..];

            int end = _metaDb.GetRow(endIndex).Location;
            return Bytes.Span[start..end];
        }

        internal Utf8Memory GetTagName(in DbRow row)
        {
            ReadOnlyMemory<byte> segment = Bytes[row.Location..];
            TrySkipTagType(segment, out segment);
            TryReadStringLength(segment.Span, Options, out int consumed, out int length);
            ReadOnlyMemory<byte> slice = segment.Slice(consumed, length);
            return Utf8Memory.CreateUnsafe(slice);
        }

        internal Utf8Memory GetTagName(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            return GetTagName(row);
        }

        internal ReadOnlySpan<byte> GetTagNameSpan(in DbRow row)
        {
            ReadOnlySpan<byte> segment = Bytes.Span[row.Location..];
            TrySkipTagType(segment, out segment);
            TryReadStringLength(segment, Options, out int consumed, out int length);
            ReadOnlySpan<byte> slice = segment.Slice(consumed, length);
            return slice;
        }

        internal ReadOnlySpan<byte> GetTagNameSpan(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            return GetTagNameSpan(row);
        }

        internal NbtReadStatus TryGetTagPayload(in DbRow row, out ReadOnlyMemory<byte> result)
        {
            ReadOnlyMemory<byte> segment = Bytes[row.Location..];

            if (row.Flags.HasFlag(NbtFlags.Typed))
            {
                var status = TrySkipTagType(segment, out segment);
                if (status != NbtReadStatus.Done)
                {
                    result = default;
                    return status;
                }
            }

            if (row.Flags.HasFlag(NbtFlags.Named))
            {
                var status = TrySkipTagName(segment, Options, out segment);
                if (status != NbtReadStatus.Done)
                {
                    result = default;
                    return status;
                }
            }

            result = segment;
            return NbtReadStatus.Done;
        }

        internal NbtReadStatus TryGetTagPayloadSpan(in DbRow row, out ReadOnlySpan<byte> result)
        {
            ReadOnlySpan<byte> segment = Bytes.Span[row.Location..];

            if (row.Flags.HasFlag(NbtFlags.Typed))
            {
                var status = TrySkipTagType(segment, out segment);
                if (status != NbtReadStatus.Done)
                {
                    result = default;
                    return status;
                }
            }

            if (row.Flags.HasFlag(NbtFlags.Named))
            {
                var status = TrySkipTagName(segment, Options, out segment);
                if (status != NbtReadStatus.Done)
                {
                    result = default;
                    return status;
                }
            }

            result = segment;
            return NbtReadStatus.Done;
        }

        internal ReadOnlySpan<byte> GetTagPayloadSpan(in DbRow row)
        {
            TryGetTagPayloadSpan(row, out ReadOnlySpan<byte> payload);
            return payload;
        }

        internal ReadOnlyMemory<byte> GetUtf8Memory(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.String, row.Type);

            TryGetTagPayload(row, out ReadOnlyMemory<byte> payload);

            TryReadStringLength(payload.Span, Options, out int lengthBytes, out int stringLength);
            return payload.Slice(lengthBytes, stringLength);
        }

        internal string GetString(int index)
        {
            (int lastIndex, string? lastString) = _lastIndexAndString;
            if (lastIndex == index)
                return lastString ?? string.Empty;

            lastString = Encoding.UTF8.GetString(GetUtf8Memory(index).Span);
            _lastIndexAndString = (index, lastString);
            return lastString;
        }

        internal Utf8String GetUtf8String(int index)
        {
            (int lastIndex, Utf8String? lastString) = _lastIndexAndUtf8String;
            if (lastIndex == index)
                return lastString ?? Utf8String.Empty;

            lastString = Utf8String.Create(GetUtf8Memory(index).Span);
            _lastIndexAndUtf8String = (index, lastString);
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

        internal static int GetArrayElementSize(in DbRow row)
        {
            int elementSize = row.Type switch
            {
                NbtType.String => (sizeof(byte)),
                NbtType.ByteArray => (sizeof(sbyte)),
                NbtType.IntArray => (sizeof(int)),
                NbtType.LongArray => (sizeof(long)),
                _ => throw GetWrongTagTypeException(row.Type),
            };
            return elementSize;
        }

        internal int GetArrayElementSize(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            return GetArrayElementSize(row);
        }

        internal ReadOnlyMemory<byte> GetArrayData(int index, out NbtType tagType)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.Type;
            int elementSize = GetArrayElementSize(row);
            TryGetTagPayload(row, out ReadOnlyMemory<byte> payload);
            ReadOnlyMemory<byte> segment = payload.Slice(sizeof(int), row.CollectionLength * elementSize);
            return segment;
        }

        internal ReadOnlySpan<byte> GetArrayDataSpan(int index, out NbtType tagType)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.Type;
            int elementSize = GetArrayElementSize(row);
            TryGetTagPayloadSpan(row, out ReadOnlySpan<byte> payload);
            ReadOnlySpan<byte> segment = payload.Slice(sizeof(int), row.CollectionLength * elementSize);
            return segment;
        }

        internal bool ArraySequenceEqual(int index, ReadOnlySpan<byte> other)
        {
            throw new NotImplementedException();

            var arrayData = GetArrayData(index, out var tagType);
            if (tagType == NbtType.String)
            {

            }
            else
            {

            }

            return arrayData.Span.SequenceEqual(other);
        }

        internal sbyte GetByte(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.Byte, row.Type);

            TryGetTagPayloadSpan(row, out ReadOnlySpan<byte> payload);
            return ReadByte(payload);
        }

        internal short GetShort(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayloadSpan(row);
            return row.Type switch
            {
                NbtType.Short => ReadInt16(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(row.Type),
            };
        }

        internal int GetInt(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayloadSpan(row);
            return row.Type switch
            {
                NbtType.Int => ReadInt32(payload, Options.IsBigEndian),
                NbtType.Short => ReadInt16(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(row.Type),
            };
        }

        internal long GetLong(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayloadSpan(row);
            return row.Type switch
            {
                NbtType.Long => ReadInt64(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt32(payload, Options.IsBigEndian),
                NbtType.Short => ReadInt16(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(row.Type),
            };
        }

        internal float GetFloat(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayloadSpan(row);
            return row.Type switch
            {
                NbtType.Float => ReadFloat(payload, Options.IsBigEndian),
                NbtType.Double => (float)ReadDouble(payload, Options.IsBigEndian),
                NbtType.Long => ReadInt64(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt32(payload, Options.IsBigEndian),
                NbtType.Short => ReadInt16(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(row.Type),
            };
        }

        internal double GetDouble(int index)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayloadSpan(row);
            return row.Type switch
            {
                NbtType.Double => ReadDouble(payload, Options.IsBigEndian),
                NbtType.Float => ReadFloat(payload, Options.IsBigEndian),
                NbtType.Long => ReadInt64(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt32(payload, Options.IsBigEndian),
                NbtType.Short => ReadInt16(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(row.Type),
            };
        }

        internal NbtElement CloneTag(int index, ArrayPool<byte>? pool)
        {
            int endIndex = GetEndIndex(index);
            MetadataDb newDb = _metaDb.CopySegment(index, endIndex, _pool);

            ReadOnlySpan<byte> segment = GetRawDataSpan(index, out _);
            byte[] segmentCopy = pool != null ? pool.Rent(segment.Length) : new byte[segment.Length];
            segment.CopyTo(segmentCopy);

            var newDocument = new NbtDocument(
                segmentCopy, Options, newDb, segmentCopy, pool);

            return newDocument.RootTag;
        }

        internal void WriteTagTo(int index, NbtWriter writer)
        {
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            throw new NotImplementedException();

            switch (row.Type)
            {
                case NbtType.Compound:
                    writer.WriteCompoundStart();
                    return;

                case NbtType.List:
                    //writer.WriteListStart(row.ContainerLength, containertype);
                    return;

                case NbtType.Undefined:
                    throw new Exception($"Unexpected {row.Type} tag.");

                default:
                    return;
            }
        }

        private void WriteContainer(int index, NbtWriter writer)
        {
            int endIndex = GetEndIndex(index);

            ReadOnlySpan<byte> data = Bytes.Span;
            for (int i = index + DbRow.Size; i < endIndex; i += DbRow.Size)
            {
                ref readonly DbRow row = ref _metaDb.GetRow(index);
                throw new NotImplementedException();
                //ReadOnlySpan<byte> segment = data.Slice(row.Location, gib length here);
                //writer.WriteRaw(segment);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckExpectedType(NbtType expected, NbtType actual)
        {
            if (expected != actual)
                throw GetWrongTagTypeException(actual);
        }

        private static InvalidOperationException GetWrongTagTypeException(NbtType type)
        {
            return new InvalidOperationException($"Unexpected tag type \"{type}\".");
        }

        private static void CheckSupportedOptions(NbtOptions readerOptions, string paramName)
        {
        }
    }
}
