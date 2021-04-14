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
        public MetadataDb _metaDb;
        private byte[]? _extraRentedBytes;
        private ArrayPool<byte>? _pool;
        private (int, string?) _lastIndexAndString = (-1, null);
        private (int, Utf8String?) _lastIndexAndUtf8String = (-1, null);

        internal bool IsDisposable => _pool != null;

        public NbtOptions Options { get; }

        public int ByteLength => _data.Length;

        /// <summary>
        /// The <see cref="NbtElement"/> representing the value of the document.
        /// </summary>
        public NbtElement RootTag => new NbtElement(this, 0);

        private NbtDocument(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            MetadataDb parsedData,
            byte[]? extraRentedBytes,
            ArrayPool<byte>? pool)
        {
            Debug.Assert(!data.IsEmpty);

            _data = data;
            _metaDb = parsedData;
            _extraRentedBytes = extraRentedBytes;
            _pool = pool;
            Options = options;
            
            // extraRentedBytes better be null if we're not disposable.
            Debug.Assert(IsDisposable || extraRentedBytes == null);
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
                _pool?.Return(extraRentedBytes);
            }
        }

        internal NbtType GetTagType(int index)
        {
            AssertNotDisposed();
            return _metaDb.GetTagType(index);
        }

        internal int GetLength(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (!row.Type.IsCollection())
                throw new Exception("The tag is not a collection.");

            return row.CollectionLength;
        }

        internal NbtType GetListType(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            if (row.Type != NbtType.List)
                throw new Exception("The tag is not a list.");

            ReadOnlyMemory<byte> payload = GetTagPayload(row);
            return (NbtType)payload.Span[0];
        }

        internal NbtFlags GetFlags(int index)
        {
            AssertNotDisposed();
            return _metaDb.GetFlags(index);
        }

        internal NbtElement GetContainerElement(int index, int arrayIndex)
        {
            AssertNotDisposed();
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

                objectOffset += _metaDb.GetRowCount(objectOffset) * DbRow.Size;
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            int endIndex = GetEndIndex(index, row);
            return endIndex;
        }

        internal ReadOnlyMemory<byte> GetRawData(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            int start = row.Location;
            int endIndex = GetEndIndex(index, row);
            if (endIndex == _metaDb.ByteLength)
                return _data[start..];

            int end = _metaDb.GetLocation(endIndex);
            return _data[start..end];
        }

        internal Utf8Memory GetTagName(in DbRow row)
        {
            if (!row.Flags.HasFlag(NbtFlags.Named))
                return Utf8Memory.Empty;

            ReadOnlyMemory<byte> segment = _data[row.Location..];

            if (row.Flags.HasFlag(NbtFlags.Typed))
                segment = SkipTagType(segment);

            int nameLength = ReadStringLength(segment.Span, Options, out int lengthBytes);
            return Utf8Memory.CreateUnsafe(segment.Slice(lengthBytes, nameLength));
        }

        internal Utf8Memory GetTagName(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            return GetTagName(row);
        }

        internal ReadOnlyMemory<byte> GetTagPayload(in DbRow row)
        {
            ReadOnlyMemory<byte> segment = _data[row.Location..];

            if (row.Flags.HasFlag(NbtFlags.Typed))
                segment = SkipTagType(segment);

            if (row.Flags.HasFlag(NbtFlags.Named))
                segment = SkipTagName(segment, Options);

            return segment;
        }

        internal ReadOnlyMemory<byte> GetUtf8Memory(int index)
        {
            AssertNotDisposed();

            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.String, row.Type);

            ReadOnlyMemory<byte> payload = GetTagPayload(row);
            ReadOnlySpan<byte> payloadSpan = payload.Span;

            int stringLength = ReadStringLength(payload.Span, Options, out int lengthBytes);
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            return GetArrayElementSize(row);
        }

        internal ReadOnlyMemory<byte> GetArrayData(int index, out NbtType tagType)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            tagType = row.Type;
            int elementSize = GetArrayElementSize(row);
            ReadOnlyMemory<byte> payload = GetTagPayload(row);
            var segment = payload.Slice(sizeof(int), row.CollectionLength * elementSize);
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);
            CheckExpectedType(NbtType.Byte, row.Type);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            return ReadByte(payload);
        }

        internal short GetShort(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            var type = row.Type;
            return type switch
            {
                NbtType.Short => ReadShort(payload, Options.IsBigEndian),
                NbtType.Byte => ReadByte(payload),
                _ => throw GetWrongTagTypeException(type),
            };
        }

        internal int GetInt(int index)
        {
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            var type = row.Type;
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            var type = row.Type;
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            var type = row.Type;
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
            AssertNotDisposed();
            ref readonly DbRow row = ref _metaDb.GetRow(index);

            ReadOnlySpan<byte> payload = GetTagPayload(row).Span;
            var type = row.Type;
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
            int endIndex = GetEndIndex(index);
            MetadataDb newDb = _metaDb.CopySegment(index, endIndex, _pool);

            var segment = GetRawData(index);
            var segmentCopy = new byte[segment.Length];
            segment.CopyTo(segmentCopy);

            var newDocument = new NbtDocument(
                segmentCopy, Options, newDb, null, null);

            return newDocument.RootTag;
        }

        internal void WriteTagTo(int index, NbtWriter writer)
        {
            AssertNotDisposed();
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

            ReadOnlySpan<byte> data = _data.Span;
            for (int i = index + DbRow.Size; i < endIndex; i += DbRow.Size)
            {
                ref readonly DbRow row = ref _metaDb.GetRow(index);
                throw new NotImplementedException();
                //ReadOnlySpan<byte> segment = data.Slice(row.Location, gib length here);
                //writer.WriteRaw(segment);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertNotDisposed()
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
