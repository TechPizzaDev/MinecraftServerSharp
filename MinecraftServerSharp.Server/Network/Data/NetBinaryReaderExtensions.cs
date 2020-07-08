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
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Span<byte> buffer = stackalloc byte[4096];
            int total = 0;
            while (count > 0)
            {
                int read = reader.ReadBytes(buffer.Slice(0, Math.Min(buffer.Length, count)));
                if (read == 0)
                    break;

                output.Write(buffer.Slice(0, read));
                total += read;
                count -= read;
            }

            return total;
        }
    }
}
