using System;
using System.IO;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct VarLong
    {
        public const int MaxEncodedSize = 10;

        public readonly long Value;

        public VarLong(long value) => Value = value;

        public int Encode(Span<byte> destination)
        {
            ulong value = (ulong)Value;
            int index = 0;
            while (value >= 0x80)
            {
                destination[index++] = (byte)(value | 0x80);
                value >>= 7;
            }
            destination[index++] = (byte)value;
            return index;
        }

        public static VarLong Decode(Stream stream)
        {
            long count = 0;
            int shift = 0;
            long b;
            do
            {
                if (shift == 10 * 7)
                    throw new InvalidDataException("Shift is too big.");

                b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();

                count |= (b & 0x7F) << shift;
                shift += 7;

            } while ((b & 0x80) != 0);

            return (VarLong)count;
        }

        public static long Decode(ReadOnlySpan<byte> source, out int bytes)
        {
            bytes = 0;
            long count = 0;
            long b;
            do
            {
                if (bytes == 10)
                    throw new InvalidDataException("Shift is too big.");

                b = source[bytes];
                count |= (b & 0x7F) << (bytes * 7);
                bytes++;

            } while ((b & 0x80) != 0);

            return count;
        }

        public static implicit operator long(VarLong value) => value.Value;
        public static explicit operator VarLong(long value) => new VarLong(value);
    }
}
