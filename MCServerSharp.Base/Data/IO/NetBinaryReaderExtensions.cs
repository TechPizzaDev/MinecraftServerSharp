using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MCServerSharp.Data.IO
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


        public static OperationStatus Read(this NetBinaryReader reader, Span<int> destination)
        {
            static OperationStatus ReadBytes(NetBinaryReader reader, Span<int> destination)
            {
                return reader.Read(MemoryMarshal.AsBytes(destination));
            }

            static OperationStatus ReadReverse(NetBinaryReader reader, Span<int> destination)
            {
                Span<int> buffer = stackalloc int[Math.Min(destination.Length, 2048 / sizeof(int))];

                int offset = 0;
                do
                {
                    // TODO: vectorize

                    var slice = buffer.Slice(0, Math.Min(buffer.Length, destination.Length - offset));
                    var status = ReadBytes(reader, slice);
                    if (status != OperationStatus.Done)
                        return status;

                    for (int i = 0; i < slice.Length; i++)
                        destination[i + offset] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    offset += slice.Length;
                }
                while (offset < destination.Length);
                return OperationStatus.Done;
            }

            if (BitConverter.IsLittleEndian)
            {
                if (reader.Options.IsBigEndian)
                    return ReadReverse(reader, destination);
                else
                    return ReadBytes(reader, destination);
            }
            else
            {
                if (reader.Options.IsBigEndian)
                    return ReadBytes(reader, destination);
                else
                    return ReadReverse(reader, destination);
            }
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
