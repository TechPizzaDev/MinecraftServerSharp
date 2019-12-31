using System;
using System.IO;

namespace MinecraftServerSharp.Utility
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Removes a front portion of the memory stream.
        /// </summary>
        public static void TrimStart(this MemoryStream stream, int length)
        {
            if (length == 0)
                return;

            // Seek past the data.
            stream.Seek(length, SeekOrigin.Begin);

            // TODO: make better use of these stream instances
            int requiredSize = (int)(stream.Length - length);
            using var tmp = RecyclableMemoryManager.Default.GetStream(requiredSize);

            // Copy all the future data.
            stream.PooledCopyTo(tmp);

            // Remove all buffered data.
            stream.SetLength(0);
            stream.Capacity = 0;

            // Copy back the future data.
            tmp.WriteTo(stream);
            stream.Position = 0;
        }

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
