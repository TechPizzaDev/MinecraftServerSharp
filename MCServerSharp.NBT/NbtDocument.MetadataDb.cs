using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public partial struct MetadataDb : IDisposable
        {
            private ArrayPool<byte>? _pool;
            private byte[] _data;

            public int ByteLength { get; private set; }

            public MetadataDb(ArrayPool<byte>? pool, byte[] completeDb, int byteLength)
            {
                _pool = pool;
                _data = completeDb;
                ByteLength = byteLength;
            }

            public MetadataDb(ArrayPool<byte>? pool, int payloadLength)
            {
                // Add one tag's worth of data just because.
                int initialSize = DbRow.Size + payloadLength;

                // Stick with ArrayPool's rent/return range if it looks feasible.
                // If it's wrong, we'll just grow and copy as we would if the tags
                // were more frequent anyways.
                const int OneMegabyte = 1024 * 1024;

                if (initialSize > OneMegabyte && initialSize <= 4 * OneMegabyte)
                    initialSize = OneMegabyte;

                _pool = pool;
                _data = _pool != null ? _pool.Rent(initialSize) : new byte[initialSize];
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
                    byte[] newRent = _pool != null ? _pool.Rent(ByteLength) : new byte[ByteLength];
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
                    _pool?.Return(returnBuf);
                }
            }

            private void Enlarge()
            {
                Debug.Assert(_pool != null);

                byte[] toReturn = _data;
                int newLength = toReturn.Length * 2;
                _data = _pool != null ? _pool.Rent(newLength) : new byte[newLength];
                Buffer.BlockCopy(toReturn, 0, _data, 0, toReturn.Length);

                // The data in this rented buffer only conveys the positions and
                // lengths of tags in a document, but no content; so it does not
                // need to be cleared.
                _pool?.Return(toReturn);
            }

            [Conditional("DEBUG")]
            internal void AssertValidIndex(int index)
            {
                Debug.Assert(index >= 0);
                Debug.Assert(index <= ByteLength - DbRow.Size, $"index {index} is out of bounds");
                Debug.Assert(index % DbRow.Size == 0, $"index {index} is not at a record start position");
            }

            public ref DbRow GetRow(int index)
            {
                AssertValidIndex(index);
                return ref Unsafe.As<byte, DbRow>(ref _data[index]);
            }

            public MetadataDb CopySegment(int startIndex, int endIndex, ArrayPool<byte>? pool)
            {
                Debug.Assert(
                    endIndex > startIndex,
                    $"endIndex={endIndex} was at or before startIndex={startIndex}");

                AssertValidIndex(startIndex);
                Debug.Assert(endIndex <= ByteLength);

                int length = endIndex - startIndex;

                byte[] newDatabase = pool != null ? pool.Rent(length) : new byte[length];
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
