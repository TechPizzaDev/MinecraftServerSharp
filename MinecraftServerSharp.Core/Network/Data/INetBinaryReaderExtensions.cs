using System;
using System.IO;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network.Data
{
    public static class INetBinaryReaderExtensions
    {
        public static byte[] ReadBytes(this INetBinaryReader reader, int count)
        {
            byte[] result = new byte[count];
            int read = reader.Read(result);
            if (read != result.Length)
            {
                byte[] copy = new byte[read];
                Buffer.BlockCopy(result, 0, copy, 0, read);
                result = copy;
            }
            return result;
        }

        public static int ReadBytes(this INetBinaryReader reader, int count, Stream output)
        {
            byte[] buffer = RecyclableMemoryManager.Default.GetBlock();
            try
            {
                int numRead = 0;
                do
                {
                    int n = reader.Read(buffer.AsSpan(0, count));
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

        public static char[] ReadChars(this INetBinaryReader reader, int count)
        {
            char[] result = new char[count];
            int read = reader.Read(result);
            if (read != result.Length)
            {
                char[] copy = new char[read];
                Buffer.BlockCopy(result, 0, copy, 0, read * sizeof(char));
                result = copy;
            }
            return result;
        }
    }
}
