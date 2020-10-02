using System;
using System.IO;

namespace MCServerSharp.Utility
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Removes a front portion of the memory stream.
        /// </summary>
        public static void TrimStart(this ChunkedMemoryStream stream, int length)
        {
            // TODO: change RecyclableMemoryStream to allow every block to have
            // an offset, so we don't need to shift data

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            stream.Position = 0;

            if (length == 0)
                return;

            var helper = stream.GetBlockOffset(length);
            stream.RemoveBlockRange(0, helper.Block);

            // Now we need to shift data in trailing blocks.
            for (int i = 0; i < stream.BlockCount; i++)
            {
                var currentBlock = stream.GetBlock(i);

                int previous = i - 1;
                if (previous >= 0)
                {
                    var previousBlock = stream.GetBlock(previous);

                    var bytesToMoveBack = currentBlock.Slice(0, helper.Offset);
                    var backDst = previousBlock.Slice(previousBlock.Length - helper.Offset);
                    bytesToMoveBack.CopyTo(backDst);
                }

                var bytesToShift = currentBlock.Slice(helper.Offset);
                bytesToShift.Span.CopyTo(currentBlock.Span);
            }

            stream.SetLength(stream.Length - helper.Offset);
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream
        /// using a stack-allocated buffer.
        /// </summary>
        public static void SpanCopyTo(this Stream source, Stream destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Span<byte> buffer = stackalloc byte[4096];
            int read;
            while ((read = source.Read(buffer)) != 0)
                destination.Write(buffer.Slice(0, read));
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream
        /// using a stack-allocated buffer and reporting every write.
        /// </summary>
        public static void SpanCopyTo(
            this Stream source, Stream destination, Action<int>? onWrite)
        {
            if (onWrite == null)
            {
                SpanCopyTo(source, destination);
                return;
            }

            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Span<byte> buffer = stackalloc byte[4096];
            int read;
            while ((read = source.Read(buffer)) != 0)
            {
                destination.Write(buffer.Slice(0, read));
                onWrite.Invoke(read);
            }
        }
    }
}
