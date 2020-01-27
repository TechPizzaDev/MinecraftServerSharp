using System;
using System.IO;

namespace MinecraftServerSharp.Utility
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Removes a front portion of the memory stream.
        /// </summary>
        public static void TrimStart(this RecyclableMemoryStream stream, int length)
        {
            if (length == 0)
                return;

            stream.Position = 0;

            var helper = stream.GetBlockAndOffset(length);
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
        /// Reads the bytes from the current stream and writes them to another stream,
        /// using a pooled buffer.
        /// </summary>
        public static void PooledCopyTo(this Stream source, Stream destination)
        {
            Span<byte> buffer = stackalloc byte[1024];
            int read;
            while ((read = source.Read(buffer)) != 0)
                destination.Write(buffer.Slice(0, read));
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream,
        /// using a pooled buffer and reporting every write.
        /// </summary>
        public static void PooledCopyTo(
            this Stream source, Stream destination, Action<int> onWrite)
        {
            if (onWrite == null)
            {
                PooledCopyTo(source, destination);
                return;
            }

            Span<byte> buffer = stackalloc byte[1024];
            int read;
            while ((read = source.Read(buffer)) != 0)
            {
                destination.Write(buffer.Slice(0, read));
                onWrite.Invoke(read);
            }
        }
    }
}
