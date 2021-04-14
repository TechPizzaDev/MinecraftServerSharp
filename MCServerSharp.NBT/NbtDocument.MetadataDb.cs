using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        /// <summary>
        /// The <see cref="MetadataDb"/> stores metadata about document tags
        /// in the form of one <see cref="DbRow"/> per tag,
        /// excluding <see cref="NbtType.End"/> tags.
        /// </summary>
        public struct MetadataDb : IDisposable
        {
            private ArrayPool<byte>? _pool;
            private byte[] _data;

            public int ByteLength { get; private set; }

            public MetadataDb(ArrayPool<byte> pool, byte[] completeDb, int byteLength)
            {
                Debug.Assert(pool != null);

                _pool = pool;
                _data = completeDb;
                ByteLength = byteLength;
            }

            public MetadataDb(ArrayPool<byte> pool, int payloadLength)
            {
                Debug.Assert(pool != null);

                // Add one tag's worth of data just because.
                int initialSize = DbRow.Size + payloadLength;

                // Stick with ArrayPool's rent/return range if it looks feasible.
                // If it's wrong, we'll just grow and copy as we would if the tags
                // were more frequent anyways.
                const int OneMegabyte = 1024 * 1024;

                if (initialSize > OneMegabyte && initialSize <= 4 * OneMegabyte)
                    initialSize = OneMegabyte;

                _pool = pool;
                _data = _pool.Rent(initialSize);
                ByteLength = 0;
            }

            public void Dispose()
            {
                byte[]? data = Interlocked.Exchange(ref _data, null!);
                if (data == null)
                    return;

                // The data in this rented buffer only conveys the positions and
                // lengths of tags in a document, but no content; so it does not
                // need to be cleared.
                _pool?.Return(data);
                ByteLength = 0;
            }

            public void TrimExcess()
            {
                // There's a chance that the size we have is the size we'd get for this
                // amount of usage (particularly if Enlarge ever got called); and there's
                // the small copy-cost associated with trimming anyways. 
                // "Is half-empty" is just a rough metric for "is trimming worth it?".
                if (ByteLength <= _data.Length / 2)
                {
                    byte[] newRent = _pool.Rent(ByteLength);
                    byte[] returnBuf = newRent;

                    if (newRent.Length < _data.Length)
                    {
                        Buffer.BlockCopy(_data, 0, newRent, 0, ByteLength);
                        returnBuf = _data;
                        _data = newRent;
                    }

                    // The data in this rented buffer only conveys the positions and
                    // lengths of tags in a document, but no content; so it does not
                    // need to be cleared.
                    _pool.Return(returnBuf);
                }
            }

            public int Append(
                int location, int collectionLength, int rowCount, NbtType type, NbtFlags flags)
            {
                if (ByteLength >= _data.Length - DbRow.Size)
                    Enlarge();

                var row = new DbRow(location, collectionLength, rowCount, type, flags);
                MemoryMarshal.Write(_data.AsSpan(ByteLength), ref row);
                int index = ByteLength;
                ByteLength += DbRow.Size;
                return index;
            }

            private void Enlarge()
            {
                Debug.Assert(_pool != null);

                byte[] toReturn = _data;
                _data = _pool.Rent(toReturn.Length * 2);
                Buffer.BlockCopy(toReturn, 0, _data, 0, toReturn.Length);

                // The data in this rented buffer only conveys the positions and
                // lengths of tags in a document, but no content; so it does not
                // need to be cleared.
                _pool.Return(toReturn);
            }

            [Conditional("DEBUG")]
            private void AssertValidIndex(int index)
            {
                Debug.Assert(index >= 0);
                Debug.Assert(index <= ByteLength - DbRow.Size, $"index {index} is out of bounds");
                Debug.Assert(index % DbRow.Size == 0, $"index {index} is not at a record start position");
            }

            public ref readonly DbRow GetRow(int index)
            {
                AssertValidIndex(index);
                return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, DbRow>(_data.AsSpan(index)));
            }

            public int GetLocation(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<int>(_data.AsSpan(index + DbRow.LocationOffset));
            }

            public int GetContainerLength(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<int>(_data.AsSpan(index + DbRow.LengthOffset));
            }

            public int GetRowCount(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<int>(_data.AsSpan(index + DbRow.RowCountOffset));
            }

            public NbtType GetTagType(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<NbtType>(_data.AsSpan(index + DbRow.TagTypeOffset));
            }

            public NbtFlags GetFlags(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<NbtFlags>(_data.AsSpan(index + DbRow.FlagsOffset));
            }

            public void SetRowCount(int index, int rowCount)
            {
                AssertValidIndex(index);
                Span<byte> dataPos = _data.AsSpan(index + DbRow.RowCountOffset);
                MemoryMarshal.Write(dataPos, ref rowCount);
            }

            public void SetLength(int index, int length)
            {
                AssertValidIndex(index);
                Span<byte> dataPos = _data.AsSpan(index + DbRow.LengthOffset);
                MemoryMarshal.Write(dataPos, ref length);
            }

            public MetadataDb CopySegment(int startIndex, int endIndex, ArrayPool<byte> pool)
            {
                Debug.Assert(
                    endIndex > startIndex,
                    $"endIndex={endIndex} was at or before startIndex={startIndex}");

                AssertValidIndex(startIndex);
                Debug.Assert(endIndex <= ByteLength);

                int length = endIndex - startIndex;

                byte[]? newDatabase = pool.Rent(length);
                _data.AsSpan(startIndex, length).CopyTo(newDatabase);

                Span<int> newDbInts = MemoryMarshal.Cast<byte, int>(newDatabase);
                int locationOffset = newDbInts[0];

                for (int i = (length - DbRow.Size) / sizeof(int); i >= 0; i -= DbRow.Size / sizeof(int))
                {
                    Debug.Assert(newDbInts[i] >= locationOffset);
                    newDbInts[i] -= locationOffset;
                }

                return new MetadataDb(pool, newDatabase, length);
            }
        }
    }
}
