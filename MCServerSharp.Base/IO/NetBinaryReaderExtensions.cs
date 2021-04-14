using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MCServerSharp.Data.IO
{
    public static class NetBinaryReaderExtensions
    {
        /// <summary>
        /// </summary>
        /// <remarks>
        /// Allocates array. Try to use <see cref="Span{T}"/> overload.
        /// </remarks>
        public static byte[] ReadBytes(this NetBinaryReader reader, int count)
        {
            byte[] result = new byte[count];
            reader.Read(result);
            return result;
        }

        [SkipLocalsInit]
        public static OperationStatus WriteTo(this NetBinaryReader reader, Stream output, int count)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Span<byte> buffer = stackalloc byte[4096];
            do
            {
                int toRead = Math.Min(buffer.Length, count);
                int read = reader.ReadBytes(buffer.Slice(0, toRead));
                if (read == 0)
                    break;

                output.Write(buffer.Slice(0, read));
                count -= read;
            }
            while (count > 0);

            if(count > 0)
                return OperationStatus.NeedMoreData;

            return OperationStatus.Done;
        }

        public static async ValueTask<OperationStatus> WriteToAsync(
            this NetBinaryReader reader, Stream output, int count, CancellationToken cancellationToken = default)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            byte[] bufferArray = ArrayPool<byte>.Shared.Rent(1024 * 16);
            try
            {
                Memory<byte> buffer = bufferArray;
                do
                {
                    int toRead = Math.Min(buffer.Length, count);
                    int read = await reader.ReadBytesAsync(buffer.Slice(0, toRead), cancellationToken).Unchain();
                    if (read == 0)
                        break;

                    await output.WriteAsync(buffer.Slice(0, read), cancellationToken).Unchain();
                    count -= read;
                }
                while (count > 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArray);
            }

            return count > 0 ? OperationStatus.NeedMoreData : OperationStatus.Done;
        }

        public static OperationStatus Read(this NetBinaryReader reader, Span<int> destination)
        {
            static OperationStatus ReadBytes(NetBinaryReader reader, Span<int> destination)
            {
                return reader.Read(MemoryMarshal.AsBytes(destination));
            }

            [SkipLocalsInit]
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

        [SkipLocalsInit]
        public static int TryReadBytes(this NetBinaryReader reader, int count, Stream output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Span<byte> buffer = stackalloc byte[4096];
            int total = 0;
            do
            {
                int read = reader.ReadBytes(buffer.Slice(0, Math.Min(buffer.Length, count)));
                if (read == 0)
                    break;

                output.Write(buffer.Slice(0, read));
                total += read;
                count -= read;
            }
            while (count > 0);
            return total;
        }
    }
}
