using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Collections;

namespace MCServerSharp.NBT
{
    // TODO: PARSE ENDIANNESS CORRECTLY

    public sealed partial class NbtDocument
    {
        [SkipLocalsInit]
        private static NbtDocument Parse(
            ReadOnlyMemory<byte> data,
            NbtOptions? options,
            byte[]? extraRentedBytes,
            ArrayPool<byte>? pool,
            out int bytesConsumed)
        {
            pool ??= ArrayPool<byte>.Shared;
            NbtOptions nbtOptions = options ?? NbtOptions.JavaDefault;

            ReadOnlySpan<byte> dataSpan = data.Span;
            MetadataDb database = new(pool, data.Length);

            ByteStack<NbtReader.ContainerFrame> readerStack;
            unsafe
            {
                NbtReader.ContainerFrame* stackBuffer = stackalloc NbtReader.ContainerFrame[NbtOptions.DefaultMaxDepth];
                Span<NbtReader.ContainerFrame> stackSpan = new(stackBuffer, NbtOptions.DefaultMaxDepth);
                readerStack = new(stackSpan, clearOnReturn: false);
            }
            NbtReaderState readerState = new(readerStack, nbtOptions);
            NbtReader reader = new(dataSpan, isFinalBlock: true, readerState);

            ByteStack<ContainerFrame> docStack;
            unsafe
            {
                ContainerFrame* stackBuffer = stackalloc ContainerFrame[NbtOptions.DefaultMaxDepth];
                Span<ContainerFrame> stackSpan = new(stackBuffer, NbtOptions.DefaultMaxDepth);
                docStack = new(stackSpan, clearOnReturn: false);
            }

            try
            {
                Parse(ref reader, ref database, ref docStack);
                bytesConsumed = (int)reader.BytesConsumed;
            }
            catch
            {
                database.Dispose();
                throw;
            }
            finally
            {
                readerState.Dispose();
                readerStack.Dispose();
                docStack.Dispose();
            }

            return new NbtDocument(data, nbtOptions, database, extraRentedBytes, pool);
        }

        // TODO:
        //public static NbtDocument Parse(Stream data, NbtOptions? options = default)
        //{
        //
        //}

        public static async Task<NbtDocument> ParseAsync(
            Stream data, NbtOptions? options, ArrayPool<byte>? pool, CancellationToken cancellationToken)
        {
            pool ??= ArrayPool<byte>.Shared;

            int initialBufferLength = 1024 * 64;
            if (data.CanSeek)
                initialBufferLength = (int)(data.Length - data.Position);

            byte[] bufferArray = pool.Rent(initialBufferLength);
            try
            {
                int totalRead = 0;
                int read = 0;
                do
                {
                    Memory<byte> buffer = bufferArray.AsMemory(totalRead);
                    if (buffer.Length == 0)
                    {
                        byte[] oldBufferArray = bufferArray;
                        bufferArray = pool.Rent(bufferArray.Length * 2);
                        Buffer.BlockCopy(oldBufferArray, 0, bufferArray, 0, totalRead);
                        pool.Return(oldBufferArray);
                        continue;
                    }

                    read = await data.ReadAsync(buffer, cancellationToken).Unchain();
                    totalRead += read;
                }
                while (read > 0);

                return Parse(bufferArray.AsMemory(0, totalRead), options, bufferArray, pool, out _);
            }
            catch
            {
                pool.Return(bufferArray, true);
                throw;
            }
        }

        public static NbtDocument Parse(
            Stream data, NbtOptions? options, ArrayPool<byte>? pool)
        {
            pool ??= ArrayPool<byte>.Shared;

            int initialBufferLength = 1024 * 64;
            if (data.CanSeek)
                initialBufferLength = (int)(data.Length - data.Position);

            byte[] bufferArray = pool.Rent(initialBufferLength);
            try
            {
                int totalRead = 0;
                int read = 0;
                do
                {
                    Memory<byte> buffer = bufferArray.AsMemory(totalRead);
                    if (buffer.Length == 0)
                    {
                        byte[] oldBufferArray = bufferArray;
                        bufferArray = pool.Rent(bufferArray.Length * 2);
                        Buffer.BlockCopy(oldBufferArray, 0, bufferArray, 0, totalRead);
                        pool.Return(oldBufferArray);
                        continue;
                    }

                    read = data.Read(buffer.Span);
                    totalRead += read;
                }
                while (read > 0);

                return Parse(bufferArray.AsMemory(0, totalRead), options, bufferArray, pool, out _);
            }
            catch
            {
                pool.Return(bufferArray, true);
                throw;
            }
        }

        public static NbtDocument Parse(
            ReadOnlyMemory<byte> data, out int bytesConsumed, NbtOptions? options = default, ArrayPool<byte>? pool = null)
        {
            return Parse(data, options, null, pool, out bytesConsumed);
        }

        //public static NbtDocument ParseValue(ref NbtReader reader)
        //{
        //
        //}
        //
        //public static bool TryParseValue(ref NbtReader reader, out NbtDocument document)
        //{
        //}

        private static void Parse(
            ref NbtReader reader,
            ref MetadataDb database,
            ref ByteStack<ContainerFrame> stack)
        {
            int rowCount = 0;

            while (reader.Read())
            {
                int location = reader.TagLocation;
                NbtType type = reader.TagType;
                NbtFlags flags = reader.TagFlags;

                PeekStack:
                ref ContainerFrame frame = ref stack.TryPeek();
                if (!Unsafe.IsNullRef(ref frame))
                {
                    if (frame.ListEntriesRemaining == 0)
                    {
                        stack.TryPop();

                        int totalRowCount = rowCount - frame.InitialRowCount;
                        database.SetRowCount(frame.ContainerRow, totalRowCount);
                        goto PeekStack;
                    }
                    else if (frame.ListEntriesRemaining != -1)
                    {
                        frame.ListEntriesRemaining--;
                    }
                    else
                    {
                        frame.CompoundEntryCounter++;
                    }
                }

                switch (type)
                {
                    case NbtType.End:
                    {
                        // Documents with a single End tag (no Compound root) are valid.
                        if (stack.TryPop(out ContainerFrame compoundFrame))
                        {
                            int totalRowCount = rowCount - compoundFrame.InitialRowCount;
                            int compoundLength = compoundFrame.CompoundEntryCounter - 1; // -1 to exclude End

                            database.SetRowCount(compoundFrame.ContainerRow, totalRowCount);
                            database.SetLength(compoundFrame.ContainerRow, compoundLength);
                        }
                        continue; // Continue to not increment row count
                    }

                    case NbtType.Compound:
                    {
                        int containerRow = database.Append(
                            location, collectionLength: 0, rowCount: 1, type, flags);

                        stack.Push(new ContainerFrame(containerRow, rowCount)
                        {
                            ListEntriesRemaining = -1
                        });
                        break;
                    }

                    case NbtType.List:
                    {
                        int listLength = reader.TagCollectionLength;
                        int containerRow = database.Append(
                            location, listLength, rowCount: 1, type, flags);

                        stack.Push(new ContainerFrame(containerRow, rowCount)
                        {
                            ListEntriesRemaining = listLength
                        });
                        break;
                    }

                    default:
                        database.Append(location, reader.TagCollectionLength, rowCount: 1, type, flags);
                        break;
                }

                rowCount++;
            }

            database.TrimExcess();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ContainerFrame
        {
            public int ContainerRow { get; }
            public int InitialRowCount { get; }

            public int ListEntriesRemaining;
            public int CompoundEntryCounter;

            public ContainerFrame(int containerRow, int initialRowCount)
            {
                ContainerRow = containerRow;
                InitialRowCount = initialRowCount;

                ListEntriesRemaining = default;
                CompoundEntryCounter = default;
            }
        }
    }
}
