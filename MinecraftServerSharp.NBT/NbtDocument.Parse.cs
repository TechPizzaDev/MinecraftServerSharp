using System;

namespace MinecraftServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        private static NbtDocument Parse(
            ReadOnlyMemory<byte> data,
            NbtReaderOptions readerOptions,
            byte[]? extraRentedBytes)
        {
            ReadOnlySpan<byte> dataSpan = data.Span;
            var database = new MetadataDb(data.Length);
            var stack = new RowFrameStack(NbtDocumentOptions.DefaultMaxDepth * RowFrame.Size);

            try
            {
                Parse(dataSpan, readerOptions, ref database, ref stack);
            }
            catch
            {
                database.Dispose();
                throw;
            }
            finally
            {
                stack.Dispose();
            }

            return new NbtDocument(data, database, extraRentedBytes);
        }

        //public static NbtDocument Parse(Stream data, NbtDocumentOptions options = default)
        //{
        //
        //}

        public static NbtDocument Parse(ReadOnlyMemory<byte> data, NbtDocumentOptions options = default)
        {
            return Parse(data, options.GetReaderOptions(), null);
        }
        
        //public static NbtDocument ParseValue(ref NbtReader reader)
        //{
        //
        //}
        //
        //public static bool TryParseValue(ref NbtReader reader, out NbtDocument document)
        //{
        //}
    }
}
