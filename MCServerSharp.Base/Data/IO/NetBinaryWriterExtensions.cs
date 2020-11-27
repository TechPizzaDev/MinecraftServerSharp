﻿using System;
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

                    var slice = source.Slice(offset, Math.Min(buffer.Length, source.Length - offset));
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer.Slice(0, slice.Length));
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
                    var slice = source.Slice(offset, count);
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer.Slice(0, slice.Length));
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
                    var slice = source.Slice(offset, count);
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer.Slice(0, slice.Length));
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

                    var slice = source.Slice(offset, Math.Min(buffer.Length, source.Length));
                    for (int i = 0; i < slice.Length; i++)
                        buffer[i] = BinaryPrimitives.ReverseEndianness(slice[i]);

                    WriteBytes(writer, buffer.Slice(0, slice.Length));
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
            Span<byte> buffer = stackalloc byte[2048];
            Span<byte> span = buffer;

            for (int i = 0; i < values.Length; i++)
            {
                int written = new VarInt(values[i]).Encode(span);
                span = span[written..];

                if (span.Length < VarInt.MaxEncodedSize)
                {
                    writer.Write(buffer.Slice(0, buffer.Length - span.Length));
                    span = buffer;
                }
            }

            writer.Write(buffer.Slice(0, buffer.Length - span.Length));
        }

        [SkipLocalsInit]
        public static void WriteVar(this NetBinaryWriter writer, ReadOnlySpan<long> values)
        {
            Span<byte> buffer = stackalloc byte[2048];
            Span<byte> span = buffer;

            for (int i = 0; i < values.Length; i++)
            {
                int written = new VarLong(values[i]).Encode(span);
                span = span[written..];

                if (span.Length < VarLong.MaxEncodedSize)
                {
                    writer.Write(buffer.Slice(0, buffer.Length - span.Length));
                    span = buffer;
                }
            }

            writer.Write(buffer.Slice(0, buffer.Length - span.Length));
        }
    }
}
