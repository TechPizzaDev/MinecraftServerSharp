using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MinecraftServerSharp.Collections;

namespace MinecraftServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        private static NbtDocument Parse(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            byte[]? extraRentedBytes)
        {
            ReadOnlySpan<byte> dataSpan = data.Span;
            var database = new MetadataDb(data.Length);
            var stack = new ByteStack<ContainerFrame>(NbtOptions.DefaultMaxDepth, clearOnReturn: false);

            var readerState = new NbtReaderState(options);
            var reader = new NbtReader(dataSpan, isFinalBlock: true, readerState);

            try
            {
                Parse(ref reader, ref database, ref stack);
                Debug.Assert(reader.BytesConsumed == dataSpan.Length);
            }
            catch
            {
                database.Dispose();
                throw;
            }
            finally
            {
                readerState.Dispose();
                stack.Dispose();
            }

            return new NbtDocument(data, options, database, extraRentedBytes);
        }

        //public static NbtDocument Parse(Stream data, NbtOptions? options = default)
        //{
        //
        //}

        //public static Task<NbtDocument> ParseAsync(Stream data, NbtOptions? options = default)
        //{
        //
        //}

        public static NbtDocument Parse(ReadOnlyMemory<byte> data, NbtOptions? options = default)
        {
            return Parse(data, options ?? NbtOptions.JavaDefault, null);
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
            int numberOfRows = 0;

            // Length on Compound frames decrementally counts children.
            // Length on List frames starts with the List's length end decrements towards zero.
            void UpdateLength(ref ByteStack<ContainerFrame> s, ref MetadataDb db)
            {

                // TODO: optimize stack usage 
                if (s.TryPop(out ContainerFrame frame))
                {
                    frame.Length--;

                    if (frame.Length == 0 && db.GetTagType(frame.ContainerIndex) == NbtType.List)
                        db.SetNumberOfRows(frame.ContainerIndex, numberOfRows - frame.NumberOfRows + 1);
                    else
                        s.Push(frame);
                }
            }

            while (reader.Read())
            {
                numberOfRows++;

                int tagStart = reader.TagStartIndex;
                NbtFlags tagFlags = reader.TagFlags;
                NbtType tagType = reader.TagType;
                int tagArrayLength = reader.TagArrayLength;

                switch (tagType)
                {
                    case NbtType.Compound:
                    {
                        int index = database.Append(tagStart, containerLength: 0, numberOfRows: 0, tagType, tagFlags);
                        stack.Push(new ContainerFrame(index, length: 0, numberOfRows));
                        break;
                    }

                    case NbtType.End:
                    {
                        database.Append(tagStart, reader.TagSpan.Length, numberOfRows: 1, tagType, tagFlags);

                        var compoundFrame = stack.Pop();

                        database.SetNumberOfRows(
                            compoundFrame.ContainerIndex, numberOfRows - compoundFrame.NumberOfRows + 1); // +1 for End

                        database.SetLength(compoundFrame.ContainerIndex, -compoundFrame.Length - 1); // -1 for End
                        break;
                    }

                    case NbtType.List:
                    {
                        UpdateLength(ref stack, ref database);

                        int index = database.Append(tagStart, tagArrayLength, numberOfRows: 0, tagType, tagFlags);
                        stack.Push(new ContainerFrame(index, tagArrayLength, numberOfRows));
                        continue;
                    }

                    case NbtType.String:
                    case NbtType.ByteArray:
                    case NbtType.IntArray:
                    case NbtType.LongArray:
                        database.Append(tagStart, tagArrayLength, numberOfRows: 1, tagType, tagFlags);
                        break;

                    default:
                        database.Append(tagStart, 0, numberOfRows: 1, tagType, tagFlags);
                        break;
                }

                UpdateLength(ref stack, ref database);
            }

            database.TrimExcess();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ContainerFrame
        {
            public int ContainerIndex;
            public int NumberOfRows;
            public int Length;

            public ContainerFrame(int containerIndex, int length, int numberOfRows)
            {
                ContainerIndex = containerIndex;
                NumberOfRows = numberOfRows;
                Length = length;
            }
        }
    }
}
