﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MCServerSharp.Collections;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        [SkipLocalsInit]
        private static NbtDocument Parse(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            byte[]? extraRentedBytes,
            out int bytesConsumed)
        {
            ReadOnlySpan<byte> dataSpan = data.Span;
            MetadataDb database = new(data.Length);

            ByteStack<NbtReader.ContainerFrame> readerStack;
            unsafe
            {
                NbtReader.ContainerFrame* stackBuffer = stackalloc NbtReader.ContainerFrame[NbtOptions.DefaultMaxDepth];
                Span<NbtReader.ContainerFrame> stackSpan = new(stackBuffer, NbtOptions.DefaultMaxDepth);
                readerStack = new(stackSpan, clearOnReturn: false);
            }
            NbtReaderState readerState = new(readerStack, options);
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

            return new NbtDocument(data, options, database, extraRentedBytes, isDisposable: true);
        }

        // TODO:
        //public static NbtDocument Parse(Stream data, NbtOptions? options = default)
        //{
        //
        //}

        //public static Task<NbtDocument> ParseAsync(Stream data, NbtOptions? options = default)
        //{
        //
        //}

        public static NbtDocument Parse(
            ReadOnlyMemory<byte> data, out int bytesConsumed, NbtOptions? options = default)
        {
            return Parse(data, options ?? NbtOptions.JavaDefault, null, out bytesConsumed);
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
