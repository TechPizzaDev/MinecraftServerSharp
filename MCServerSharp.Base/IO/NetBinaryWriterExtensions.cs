using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.Data.IO
{
    public static class NetBinaryWriterExtensions
    {
        // TODO: vectorize the reversed writers??

        public static void Write(this NetBinaryWriter writer, ReadOnlySpan<sbyte> values)
        {
            writer.Write(MemoryMarshal.Cast<sbyte, byte>(values));
        }

        public static void Write(this NetBinaryWriter writer, ReadOnlySpan<int> values)
        {
            static void WriteBytes(NetBinaryWriter writer, ReadOnlySpan<int> source)
            {
                writer.Write(MemoryMarshal.AsBytes(source));
            }

            [SkipLocalsInit]
            static void WriteReverse(NetBinaryWriter writer, ReadOnlySpan<int> source)
            {
                Span<int> buffer = stackalloc int[Math.Min(source.Length, 2048 / sizeof(int))];

                int offset = 0;
                do
                {
                    // TODO: vectorize

                    ReadOnlySpan<int> slice = source.Slice(offset, Math.Min(buffer.Length, source.Length - offset));
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer[..slice.Length]);
                    offset += slice.Length;
                }
                while (offset < source.Length);
            }

            if (BitConverter.IsLittleEndian)
            {
                if (writer.Options.IsBigEndian)
                    WriteReverse(writer, values);
                else
                    WriteBytes(writer, values);
            }
            else
            {
                if (writer.Options.IsBigEndian)
                    WriteBytes(writer, values);
                else
                    WriteReverse(writer, values);
            }
        }

        public static void Write(this NetBinaryWriter writer, ReadOnlySpan<uint> values)
        {
            static void WriteBytes(NetBinaryWriter writer, ReadOnlySpan<uint> source)
            {
                writer.Write(MemoryMarshal.AsBytes(source));
            }

            [SkipLocalsInit]
            static void WriteReverse(NetBinaryWriter writer, ReadOnlySpan<uint> source)
            {
                Span<uint> buffer = stackalloc uint[Math.Min(source.Length, 2048 / sizeof(uint))];

                int offset = 0;
                do
                {
                    int count = Math.Min(buffer.Length, source.Length - offset);
                    ReadOnlySpan<uint> slice = source.Slice(offset, count);
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer[..slice.Length]);
                    offset += slice.Length;

                }
                while (offset < source.Length);
            }

            if (BitConverter.IsLittleEndian)
            {
                if (writer.Options.IsBigEndian)
                    WriteReverse(writer, values);
                else
                    WriteBytes(writer, values);
            }
            else
            {
                if (writer.Options.IsBigEndian)
                    WriteBytes(writer, values);
                else
                    WriteReverse(writer, values);
            }
        }

        public static void Write(this NetBinaryWriter writer, ReadOnlySpan<ulong> values)
        {
            static void WriteBytes(NetBinaryWriter writer, ReadOnlySpan<ulong> source)
            {
                writer.Write(MemoryMarshal.AsBytes(source));
            }

            [SkipLocalsInit]
            static void WriteReverse(NetBinaryWriter writer, ReadOnlySpan<ulong> source)
            {
                Span<ulong> buffer = stackalloc ulong[Math.Min(source.Length, 2048 / sizeof(ulong))];

                int offset = 0;
                do
                {
                    int count = Math.Min(buffer.Length, source.Length - offset);
                    ReadOnlySpan<ulong> slice = source.Slice(offset, count);
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer[..slice.Length]);
                    offset += slice.Length;

                }
                while (offset < source.Length);
            }

            if (BitConverter.IsLittleEndian)
            {
                if (writer.Options.IsBigEndian)
                    WriteReverse(writer, values);
                else
                    WriteBytes(writer, values);
            }
            else
            {
                if (writer.Options.IsBigEndian)
                    WriteBytes(writer, values);
                else
                    WriteReverse(writer, values);
            }
        }

        public static void Write(this NetBinaryWriter writer, ReadOnlySpan<long> values)
        {
            static void WriteBytes(NetBinaryWriter writer, ReadOnlySpan<long> source)
            {
                writer.Write(MemoryMarshal.AsBytes(source));
            }

            [SkipLocalsInit]
            static void WriteReverse(NetBinaryWriter writer, ReadOnlySpan<long> source)
            {
                Span<long> buffer = stackalloc long[Math.Min(source.Length, 2048 / sizeof(ulong))];

                int offset = 0;
                while (offset < source.Length)
                {
                    // TODO: vectorize

                    ReadOnlySpan<long> slice = source.Slice(offset, Math.Min(buffer.Length, source.Length));
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer[..slice.Length]);
                    offset += slice.Length;
                }
            }

            if (BitConverter.IsLittleEndian)
            {
                if (writer.Options.IsBigEndian)
                    WriteReverse(writer, values);
                else
                    WriteBytes(writer, values);
            }
            else
            {
                if (writer.Options.IsBigEndian)
                    WriteBytes(writer, values);
                else
                    WriteReverse(writer, values);
            }
        }

        [SkipLocalsInit]
        public static void WriteVar(this NetBinaryWriter writer, ReadOnlySpan<int> values)
        {
            const int MaxCount = 512;
            Span<byte> buffer = stackalloc byte[VarInt.MaxEncodedSize * MaxCount];
            ref byte destination = ref MemoryMarshal.GetReference(buffer);
            ref byte dst = ref destination;

            while (values.Length >= MaxCount)
            {
                ReadOnlySpan<int> slice = values[..MaxCount];
                for (int i = 0; i < slice.Length; i++)
                {
                    dst = ref new VarInt(slice[i]).EncodeUnsafe(ref dst);
                }

                writer.Write(buffer[..(int)Unsafe.ByteOffset(ref destination, ref dst)]);
                dst = ref destination;

                values = values[MaxCount..];
            }

            for (int i = 0; i < values.Length; i++)
            {
                dst = ref new VarInt(values[i]).EncodeUnsafe(ref dst);
            }

            writer.Write(buffer[..(int)Unsafe.ByteOffset(ref destination, ref dst)]);
        }

        public static void WriteVar(this NetBinaryWriter writer, ReadOnlySpan<uint> values)
        {
            WriteVar(writer, MemoryMarshal.Cast<uint, int>(values));
        }

        [SkipLocalsInit]
        public static void WriteVar(this NetBinaryWriter writer, ReadOnlySpan<long> values)
        {
            const int MaxCount = 256;
            Span<byte> buffer = stackalloc byte[VarLong.MaxEncodedSize * MaxCount];
            ref byte destination = ref MemoryMarshal.GetReference(buffer);
            ref byte dst = ref destination;

            while (values.Length >= MaxCount)
            {
                ReadOnlySpan<long> slice = values[..MaxCount];
                for (int i = 0; i < slice.Length; i++)
                {
                    dst = ref new VarLong(slice[i]).EncodeUnsafe(ref dst);
                }

                writer.Write(buffer[..(int)Unsafe.ByteOffset(ref destination, ref dst)]);
                dst = ref destination;

                values = values[MaxCount..];
            }

            for (int i = 0; i < values.Length; i++)
            {
                dst = new VarLong(values[i]).EncodeUnsafe(ref dst);
            }

            writer.Write(buffer[..(int)Unsafe.ByteOffset(ref destination, ref dst)]);
        }

        public static void WriteVar(this NetBinaryWriter writer, ReadOnlySpan<ulong> values)
        {
            WriteVar(writer, MemoryMarshal.Cast<ulong, long>(values));
        }
    }
}
