using System;
using System.IO;
using MinecraftServerSharp.Network.Data;

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

        public static ReadCode TryDecode(Stream stream, out VarLong result, out int bytes)
        {
            long count = 0;
            int shift = 0;
            long b;
            do
            {
                if (shift == MaxEncodedSize)
                {
                    result = default;
                    bytes = -1;
                    return ReadCode.InvalidData;
                }

                b = stream.ReadByte();
                if (b == -1)
                {
                    result = default;
                    bytes = shift;
                    return ReadCode.EndOfStream;
                }

                count |= (b & 0x7F) << (shift * 7);
                shift++;

            } while ((b & 0x80) != 0);

            result = (VarLong)count;
            bytes = shift;
            return ReadCode.Ok;
        }

        public static implicit operator long(VarLong value) => value.Value;
        public static explicit operator VarLong(long value) => new VarLong(value);
    }
}
