using System;
using System.IO;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct VarInt32
    {
        public const int MaxEncodedSize = 5;

        public readonly int Value;

        public VarInt32(int value) => Value = value;

        public int Encode(Span<byte> destination)
        {
            uint value = (uint)Value;
            int index = 0;
            while (value >= 0x80)
            {
                destination[index++] = (byte)(value | 0x80);
                value >>= 7;
            }
            destination[index++] = (byte)value;
            return index;
        }

        public static bool TryDecode(Stream stream, out VarInt32 result, out int bytes)
        {
            int count = 0;
            int shift = 0;
            int b;
            do
            {
                if (shift == 5)
                    throw new InvalidDataException("Shift is too big.");

                b = stream.ReadByte();
                if (b == -1)
                {
                    result = default;
                    bytes = shift;
                    return false;
                }

                count |= (b & 0x7F) << (shift * 7);
                shift++;

            } while ((b & 0x80) != 0);

            result = count;
            bytes = shift;
            return true;
        }

        public static int Decode(ReadOnlySpan<byte> source, out int bytes)
        {
            bytes = 0;
            int count = 0;
            byte b;
            do
            {
                if (bytes == 5)
                    throw new InvalidDataException("Shift is too big.");

                b = source[bytes];
                count |= (b & 0x7F) << (bytes * 7);
                bytes++;

            } while ((b & 0x80) != 0);

            return count;
        }

        public static implicit operator int(VarInt32 value) => value.Value;
        public static implicit operator VarInt32(int value) => new VarInt32(value);
    }
}
