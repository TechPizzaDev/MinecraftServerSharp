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
        private ReadOnlyMemory<byte> _data;
        private MetadataDb _metaDb;
        private byte[]? _extraRentedBytes;
        private (int, string?) _lastIndexAndString = (-1, null);

        internal bool IsDisposable { get; }

        public NbtOptions Options { get; }

        /// <summary>
        /// The <see cref="NbtElement"/> representing the value of the document.
        /// </summary>
        public NbtElement RootTag => new NbtElement(this, 0);

        private NbtDocument(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            MetadataDb parsedData,
            byte[]? extraRentedBytes,
            bool isDisposable = true)
        {
            Debug.Assert(!data.IsEmpty);

            _data = data;
            _metaDb = parsedData;
            _extraRentedBytes = extraRentedBytes;
            Options = options;
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

        internal int GetLength(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (!row.TagType.IsArrayLike())
                throw new Exception("The tag is not an array-like type.");

            return row.ContainerLength;
        }

        internal NbtType GetListType(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (row.TagType != NbtType.List)
                throw new Exception("The tag is not a list.");

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            return (NbtType)payload[0];
        }

        internal NbtFlags GetFlags(int index)
        {
            CheckNotDisposed();
            return _metaDb.GetFlags(index);
        }

        internal NbtElement GetContainerElement(int index, int arrayIndex)
        {
            CheckNotDisposed();
            ref readonly DbRow containerRow = ref _metaDb.GetRow(index);
            if (!containerRow.IsContainerType)
                throw new InvalidOperationException("The tag is not a container.");

            int length = containerRow.ContainerLength;
            if ((uint)arrayIndex >= (uint)length)
                throw new IndexOutOfRangeException();

            int elementCount = 0;
            int objectOffset = index + DbRow.Size;

            for (; objectOffset < _metaDb.ByteLength;)
            {
                if (arrayIndex == elementCount)
                    return new NbtElement(this, objectOffset);

                objectOffset += _metaDb.GetNumberOfRows(objectOffset) * DbRow.Size;
                elementCount++;
            }

            Debug.Fail(
                $"Ran out of database searching for array index " +
                $"{arrayIndex} from {index} when length was {length}");

            throw new IndexOutOfRangeException();
        }

        internal static int GetEndIndex(int baseIndex, in DbRow row, bool includeEndTag)
        {
            if (row.IsPrimitiveType)
                return baseIndex + DbRow.Size;

            int endIndex = baseIndex + row.NumberOfRows * DbRow.Size;

            if (!includeEndTag && row.TagType == NbtType.Compound)
                endIndex -= DbRow.Size;

            return endIndex;
        }

        internal int GetEndIndex(int index, bool includeEndTag)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            int endIndex = GetEndIndex(index, row, includeEndTag);
            return endIndex;
        }

        private ReadOnlyMemory<byte> GetRawData(int index, bool includeEndTag)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (row.IsPrimitiveType)
                return _data.Slice(row.Location, row.ContainerLength);

            int endIndex = GetEndIndex(index, row, includeEndTag);
            int start = row.Location;
            int end = _metaDb.GetLocation(endIndex);
            return _data[start..end];
        }

        internal ReadOnlySpan<byte> GetTagName(in DbRow row)
        {
            if (!row.Flags.HasFlag(NbtFlags.Named))
                return ReadOnlySpan<byte>.Empty;

            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);

            if (row.Flags.HasFlag(NbtFlags.Typed))
                segment = SkipTagType(segment);

            int nameLength = ReadStringLength(segment, Options, out int lengthBytes);
            return segment.Slice(lengthBytes, nameLength);
        }

        internal ReadOnlySpan<byte> GetTagName(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            return GetTagName(row);
        }

        internal ReadOnlySpan<byte> GetTagPayload(in DbRow row)
        {
            ReadOnlySpan<byte> data = _data.Span;
            ReadOnlySpan<byte> segment = data.Slice(row.Location);

            if (row.Flags.HasFlag(NbtFlags.Typed))
                segment = SkipTagType(segment);

            if (row.Flags.HasFlag(NbtFlags.Named))
                segment = SkipTagName(segment, Options);

            return segment;
        }

        internal string GetString(int index)
        {
            CheckNotDisposed();

            (int lastIndex, string? lastString) = _lastIndexAndString;
            if (lastIndex == index)
                return lastString ?? string.Empty;

            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.String, row.TagType);

            ReadOnlySpan<byte> payload = GetTagPayload(row);

            int stringLength = ReadStringLength(payload, Options, out int lengthBytes);
            lastString = Encoding.UTF8.GetString(payload.Slice(lengthBytes, stringLength));

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

        internal ReadOnlyMemory<byte> GetArrayData(int index, out NbtType tagType)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.TagType;

            ReadOnlySpan<byte> payload = GetTagPayload(row);

            throw new NotImplementedException();

            // TODO: return span with array length
            var segment = tagType switch
            {
                NbtType.String => _data.Slice(row.Location, row.ContainerLength * sizeof(byte)),
                NbtType.ByteArray => _data.Slice(row.Location, row.ContainerLength * sizeof(sbyte)),
                NbtType.IntArray => _data.Slice(row.Location, row.ContainerLength * sizeof(int)),
                NbtType.LongArray => _data.Slice(row.Location, row.ContainerLength * sizeof(long)),
                _ => throw GetWrongTagTypeException(tagType),
            };
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
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.Byte, row.TagType);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            return ReadByte(payload);
        }

        internal short GetShort(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            var type = row.TagType;
            return type switch
            {
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal int GetInt(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            var type = row.TagType;
            return type switch
            {
                NbtType.Int => ReadInt(payload, Options.IsBigEndian),
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal long GetLong(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            var type = row.TagType;
            return type switch
            {
                NbtType.Long => ReadLong(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt(payload, Options.IsBigEndian),
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal float GetFloat(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            var type = row.TagType;
            return type switch
            {
                NbtType.Float => ReadFloat(payload, Options.IsBigEndian),
                NbtType.Double => (float)ReadDouble(payload, Options.IsBigEndian),
                NbtType.Long => ReadLong(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt(payload, Options.IsBigEndian),
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal double GetDouble(int index)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row);
            var type = row.TagType;
            return type switch
            {
                NbtType.Double => ReadDouble(payload, Options.IsBigEndian),
                NbtType.Float => ReadFloat(payload, Options.IsBigEndian),
                NbtType.Long => ReadLong(payload, Options.IsBigEndian),
                NbtType.Int => ReadInt(payload, Options.IsBigEndian),
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal NbtElement CloneTag(int index)
        {
            int endIndex = GetEndIndex(index, includeEndTag: true);
            MetadataDb newDb = _metaDb.CopySegment(index, endIndex);

            var segment = GetRawData(index, includeEndTag: true);
            var segmentCopy = new byte[segment.Length];
            segment.CopyTo(segmentCopy);

            var newDocument = new NbtDocument(
                segmentCopy, Options, newDb, null, isDisposable: false);

            return newDocument.RootTag;
        }

        internal void WriteTagTo(int index, NbtWriter writer)
        {
            CheckNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            throw new NotImplementedException();

            switch (row.TagType)
            {
                case NbtType.Compound:
                    writer.WriteCompoundStart(row.ContainerLength);
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
            int endIndex = GetEndIndex(index, includeEndTag: true);

            ReadOnlySpan<byte> data = _data.Span;
            for (int i = index + DbRow.Size; i < endIndex; i += DbRow.Size)
            {
                ref readonly DbRow row = ref _metaDb.GetRow(index);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotDisposed()
        {
            if (_data.IsEmpty)
                throw new ObjectDisposedException(nameof(NbtDocument));
        }

        private static void CheckExpectedType(NbtType expected, NbtType actual)
        {
            if (expected != actual)
                throw GetWrongTagTypeException(actual);
        }

        private static InvalidOperationException GetWrongTagTypeException(NbtType type)
        {
            throw new InvalidOperationException($"Unexpected tag type \"{type}\".");
        }

        private static void CheckSupportedOptions(NbtOptions readerOptions, string paramName)
        {
        }
    }
}
