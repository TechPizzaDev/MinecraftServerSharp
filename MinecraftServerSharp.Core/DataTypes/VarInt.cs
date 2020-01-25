using System;
using System.IO;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct VarInt
    {
        public const int MaxEncodedSize = 5;

        public readonly int Value;

        public VarInt(int value) => Value = value;

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

        public static ReadCode TryDecode(Stream stream, out VarInt result, out int bytes)
        {
            int count = 0;
            int shift = 0;
            int b;
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

            result = (VarInt)count;
            bytes = shift;
            return ReadCode.Ok;
        }

        public static implicit operator int(VarInt value) => value.Value;
        public static explicit operator VarInt(int value) => new VarInt(value);
    }
}
