using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MCServerSharp
{
    [DebuggerDisplay("{Value, nq}")]
    public readonly struct VarInt
    {
        public const int MinEncodedSize = 1;
        public const int MaxEncodedSize = 5;

        public int Value { get; }

        public VarInt(int value)
        {
            Value = value;
        }

        public static int GetEncodedSize(uint value)
        {
            uint v = value;
            int index = 1;
            while (v >= 0x80)
            {
                v >>= 7;
                index++;
            }
            return index;
        }

        public static int GetEncodedSize(int value)
        {
            return GetEncodedSize((uint)value);
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

        public static OperationStatus TryDecode(
            ReadOnlySpan<byte> source, out VarInt result, out int bytesConsumed)
        {
            result = 0;
            bytesConsumed = 0;
            uint value = 0;
            uint b;
            do
            {
                if (bytesConsumed == MaxEncodedSize)
                    return OperationStatus.InvalidData;
                else if (source.Length - bytesConsumed <= 0)
                    return OperationStatus.NeedMoreData;

                b = source[bytesConsumed];

                value |= (b & 0x7F) << (bytesConsumed * 7);
                bytesConsumed++;
            }
            while ((b & 0x80) != 0);

            result = (int)value;
            return OperationStatus.Done;
        }

        public static OperationStatus TryDecode(
            Stream stream, out VarInt result, out int bytesConsumed)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            result = default;
            bytesConsumed = 0;
            uint value = 0;
            uint b;
            do
            {
                if (bytesConsumed == MaxEncodedSize)
                {
                    return OperationStatus.InvalidData;
                }
                else
                {
                    b = (uint)stream.ReadByte();
                    if (b == uint.MaxValue)
                        return OperationStatus.NeedMoreData;
                }

                value |= (b & 0x7F) << (bytesConsumed * 7);
                bytesConsumed++;

            } while ((b & 0x80) != 0);

            result = (int)value;
            return OperationStatus.Done;
        }

        public string ToString(IFormatProvider? provider)
        {
            return Value.ToString(provider);
        }

        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        public static implicit operator int(VarInt value) => value.Value;
        public static implicit operator VarLong(VarInt value) => new(value.Value);
        public static implicit operator VarInt(int value) => new(value);
    }
}
