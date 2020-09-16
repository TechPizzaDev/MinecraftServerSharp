using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MCServerSharp.Collections;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        private static NbtDocument Parse(
            ReadOnlyMemory<byte> data,
            NbtOptions options,
            byte[]? extraRentedBytes,
            out int bytesConsumed)
        {
            ReadOnlySpan<byte> dataSpan = data.Span;
            var database = new MetadataDb(data.Length);
            var stack = new ByteStack<ContainerFrame>(NbtOptions.DefaultMaxDepth, clearOnReturn: false);

            var readerState = new NbtReaderState(options);
            var reader = new NbtReader(dataSpan, isFinalBlock: true, readerState);

            try
            {
                Parse(ref reader, ref database, ref stack);
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
                stack.Dispose();
            }

            return new NbtDocument(data, options, database, extraRentedBytes);
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

        /// <summary>
        /// Length on Compound frames decrementally counts children.
        /// Length on List frames starts with the List's length end decrements towards zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateLength(
            int numberOfRows, ref ByteStack<ContainerFrame> stack, ref MetadataDb database)
        {
            // TODO: optimize stack usage (maybe by using ref?)
            if (stack.TryPop(out ContainerFrame frame))
            {
                frame.Length--;

                if (frame.Length == 0 && database.GetTagType(frame.ContainerIndex) == NbtType.List)
                    database.SetNumberOfRows(frame.ContainerIndex, numberOfRows - frame.NumberOfRows + 1);
                else
                    stack.Push(frame);
            }
        }

        private static void Parse(
            ref NbtReader reader,
            ref MetadataDb database,
            ref ByteStack<ContainerFrame> stack)
        {
            int numberOfRows = 0;

            while (reader.Read())
            {
                numberOfRows++;

                int location = reader.TagLocation;
                NbtFlags flags = reader.TagFlags;
                NbtType type = reader.TagType;
                int arrayLength = reader.TagArrayLength;

                switch (type)
                {
                    case NbtType.Compound:
                    {
                        int index = database.Append(location, containerLength: 0, numberOfRows: 0, type, flags);
                        stack.Push(new ContainerFrame(index, length: 0, numberOfRows));
                        break;
                    }

                    case NbtType.End:
                    {
                        database.Append(location, reader.TagSpan.Length, numberOfRows: 1, type, flags);

                        // Documents with a single End tag are valid.
                        var compoundFrame = stack.IsEmpty ? default : stack.Pop();

                        database.SetNumberOfRows(
                            compoundFrame.ContainerIndex, numberOfRows - compoundFrame.NumberOfRows + 1); // +1 for End

                        database.SetLength(compoundFrame.ContainerIndex, -compoundFrame.Length - 1); // -1 for End
                        break;
                    }

                    case NbtType.List:
                    {
                        UpdateLength(numberOfRows, ref stack, ref database);

                        int index = database.Append(location, arrayLength, numberOfRows: 0, type, flags);
                        stack.Push(new ContainerFrame(index, arrayLength, numberOfRows));
                        continue;
                    }

                    case NbtType.String:
                    case NbtType.ByteArray:
                    case NbtType.IntArray:
                    case NbtType.LongArray:
                        database.Append(location, arrayLength, numberOfRows: 1, type, flags);
                        break;

                    default:
                        database.Append(location, 0, numberOfRows: 1, type, flags);
                        break;
                }

                UpdateLength(numberOfRows, ref stack, ref database);
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
