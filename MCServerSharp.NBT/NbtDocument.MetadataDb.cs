using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        private struct MetadataDb : IDisposable
        {
            private byte[] _data;

            public int ByteLength { get; private set; }

            public MetadataDb(byte[] completeDb, int byteLength)
            {
                _data = completeDb;
                ByteLength = byteLength;
            }

            public MetadataDb(int payloadLength)
            {
                // Assume that a tag happens approximately every 12 bytes.
                // int estimatedTags = payloadLength / 12
                // now acknowledge that the number of bytes we need per tag is 12.
                // So that's just the payload length.
                //
                // Add one tag's worth of data just because.
                int initialSize = DbRow.Size + payloadLength;

                // Stick with ArrayPool's rent/return range if it looks feasible.
                // If it's wrong, we'll just grow and copy as we would if the tags
                // were more frequent anyways.
                const int OneMegabyte = 1024 * 1024;

                if (initialSize > OneMegabyte && initialSize <= 4 * OneMegabyte)
                    initialSize = OneMegabyte;

                _data = ArrayPool<byte>.Shared.Rent(initialSize);
                ByteLength = 0;
            }

            public MetadataDb(MetadataDb source, bool useArrayPools)
            {
                ByteLength = source.ByteLength;

                if (useArrayPools)
                {
                    _data = ArrayPool<byte>.Shared.Rent(ByteLength);
                    source._data.AsSpan(0, ByteLength).CopyTo(_data);
                }
                else
                {
                    _data = source._data.AsSpan(0, ByteLength).ToArray();
                }
            }

            public void Dispose()
            {
                byte[]? data = Interlocked.Exchange(ref _data, null!);
                if (data == null)
                    return;

                // The data in this rented buffer only conveys the positions and
                // lengths of tags in a document, but no content; so it does not
                // need to be cleared.
                ArrayPool<byte>.Shared.Return(data);
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
                    byte[] newRent = ArrayPool<byte>.Shared.Rent(ByteLength);
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
                    ArrayPool<byte>.Shared.Return(returnBuf);
                }
            }

            public int Append(int location, int containerLength, int numberOfRows, NbtType tagType, NbtFlags flags)
            {
                if (ByteLength >= _data.Length - DbRow.Size)
                    Enlarge();

                var row = new DbRow(location, containerLength, numberOfRows, tagType, flags);
                MemoryMarshal.Write(_data.AsSpan(ByteLength), ref row);
                int index = ByteLength;
                ByteLength += DbRow.Size;
                return index;
            }

            private void Enlarge()
            {
                byte[] toReturn = _data;
                _data = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);
                Buffer.BlockCopy(toReturn, 0, _data, 0, toReturn.Length);

                // The data in this rented buffer only conveys the positions and
                // lengths of tags in a document, but no content; so it does not
                // need to be cleared.
                ArrayPool<byte>.Shared.Return(toReturn);
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

            public int GetNumberOfRows(int index)
            {
                AssertValidIndex(index);
                return MemoryMarshal.Read<int>(_data.AsSpan(index + DbRow.NumberOfRowsOffset));
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

            public void SetNumberOfRows(int index, int numberOfRows)
            {
                AssertValidIndex(index);
                Span<byte> dataPos = _data.AsSpan(index + DbRow.NumberOfRowsOffset);
                MemoryMarshal.Write(dataPos, ref numberOfRows);
            }

            public void SetLength(int index, int length)
            {
                AssertValidIndex(index);
                Span<byte> dataPos = _data.AsSpan(index + DbRow.LengthOffset);
                MemoryMarshal.Write(dataPos, ref length);
            }

            public MetadataDb CopySegment(int startIndex, int endIndex)
            {
                Debug.Assert(
                    endIndex > startIndex,
                    $"endIndex={endIndex} was at or before startIndex={startIndex}");

                AssertValidIndex(startIndex);
                Debug.Assert(endIndex <= ByteLength);

                int length = endIndex - startIndex;

                var newDatabase = ArrayPool<byte>.Shared.Rent(length);
                _data.AsSpan(startIndex, length).CopyTo(newDatabase);

                Span<int> newDbInts = MemoryMarshal.Cast<byte, int>(newDatabase);
                int locationOffset = newDbInts[0];

                for (int i = (length - DbRow.Size) / sizeof(int); i >= 0; i -= DbRow.Size / sizeof(int))
                {
                    Debug.Assert(newDbInts[i] >= locationOffset);
                    newDbInts[i] -= locationOffset;
                }

                return new MetadataDb(newDatabase, length);
            }
        }
    }
}
