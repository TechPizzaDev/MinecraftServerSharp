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
        public const int MaxEncodedSize = 5;

        public int Value { get; }

        public VarInt(int value)
        {
            Value = value;
        }

        public static int GetEncodedSize(int value)
        {
            uint v = (uint)value;
            int index = 0;
            while (v >= 0x80)
            {
                v >>= 7;
                index++;
            }
            return index + 1;
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
            bytesConsumed = 0;
            int value = 0;
            int b;
            do
            {
                if (bytesConsumed == MaxEncodedSize)
                {
                    result = default;
                    return OperationStatus.InvalidData;
                }

                if (source.Length - bytesConsumed <= 0)
                {
                    result = default;
                    return OperationStatus.NeedMoreData;
                }
                b = source[bytesConsumed];

                value |= (b & 0x7F) << (bytesConsumed * 7);
                bytesConsumed++;

            } while ((b & 0x80) != 0);

            result = (VarInt)value;
            return OperationStatus.Done;
        }

        public static OperationStatus TryDecode(
            Stream stream, out VarInt result, out int bytesConsumed)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            bytesConsumed = 0;
            int value = 0;
            int b;
            do
            {
                if (bytesConsumed == MaxEncodedSize)
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

                value |= (b & 0x7F) << (bytesConsumed * 7);
                bytesConsumed++;

            } while ((b & 0x80) != 0);

            result = (VarInt)value;
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
        public static implicit operator VarInt(int value) => new VarInt(value);
    }
}
