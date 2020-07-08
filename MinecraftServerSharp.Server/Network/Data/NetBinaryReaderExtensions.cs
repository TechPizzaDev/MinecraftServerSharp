using System;
using System.IO;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network.Data
{
    public static class NetBinaryReaderExtensions
    {
        public static void Read(this NetBinaryReader reader, Span<byte> buffer)
        {
            if (reader.ReadBytes(buffer) != buffer.Length)
                throw new EndOfStreamException();
        }

        [Obsolete("Allocates array. Try to use Span<byte> overload.")]
        public static byte[] ReadBytes(this NetBinaryReader reader, int count)
        {
            byte[] result = new byte[count];
            reader.Read(result);
            return result;
        }

        public static int TryReadBytes(this NetBinaryReader reader, int count, Stream output)
        {
            byte[] buffer = RecyclableMemoryManager.Default.GetBlock();
            try
            {
                int numRead = 0;
                do
                {
                    int n = reader.ReadBytes(buffer.AsSpan(0, count));
                    if (n == 0)
                        break;

                    output.Write(buffer, 0, n);
                    numRead += n;
                    count -= n;
                } while (count > 0);

                return numRead;
            }
            finally
            {
                RecyclableMemoryManager.Default.ReturnBlock(buffer);
            }
        }
    }
}
