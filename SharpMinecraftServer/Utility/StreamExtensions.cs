using System;
using System.IO;

namespace SharpMinecraftServer.Utility
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream,
        /// using a pooled buffer.
        /// </summary>
        public static void PooledCopyTo(this Stream source, Stream destination)
        {
            byte[] buffer = RecyclableMemoryManager.Default.GetBlock();
            try
            {
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                    destination.Write(buffer, 0, read);
            }
            finally
            {
                RecyclableMemoryManager.Default.ReturnBlock(buffer);
            }
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

            byte[] buffer = RecyclableMemoryManager.Default.GetBlock();
            try
            {
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                {
                    destination.Write(buffer, 0, read);
                    onWrite.Invoke(read);
                }
            }
            finally
            {
                RecyclableMemoryManager.Default.ReturnBlock(buffer);
            }
        }
    }
}
