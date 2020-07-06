using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp
{
    [DebuggerDisplay("{Value, nq}")]
    public readonly struct VarInt
    {
        public const int MaxEncodedSize = 5;

        public int Value { get; }

        public VarInt(int value)
        {
            Value = value;
        }

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

        public static OperationStatus TryDecode(Stream stream, out VarInt result, out int bytes)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            bytes = 0;
            int count = 0;
            int b;
            do
            {
                if (bytes == MaxEncodedSize)
                {
                    result = default;
                    return OperationStatus.InvalidData;
                }

                b = stream.ReadByte();
                if (b == -1)
                {
                    result = default;
                    return OperationStatus.NeedMoreData;
                }

                count |= (b & 0x7F) << (bytes * 7);
                bytes++;

            } while ((b & 0x80) != 0);

            result = (VarInt)count;
            return OperationStatus.Done;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator int(VarInt value) => value.Value;
        public static explicit operator VarInt(int value) => new VarInt(value);
    }
}
