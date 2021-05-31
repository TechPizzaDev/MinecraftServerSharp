using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace MCServerSharp
{
    [DebuggerDisplay("{Value, nq}")]
    public readonly struct VarLong
    {
        public const int MinEncodedSize = 1;
        public const int MaxEncodedSize = 10;

        public long Value { get; }

        public VarLong(long value)
        {
            Value = value;
        }

        public static int GetEncodedSize(ulong value)
        {
            ulong v = value;
            int index = 1;
            while (v >= 0x80)
            {
                v >>= 7;
                index++;
            }
            return index;
        }

        public static int GetEncodedSize(long value)
        {
            return GetEncodedSize((ulong)value);
        }

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

        public void EncodeUnsafe(ref int index, ref byte destination)
        {
            ulong value = (ulong)Value;
            while (value >= 0x80)
            {
                Unsafe.Add(ref destination, index++) = (byte)(value | 0x80);
                value >>= 7;
            }
            Unsafe.Add(ref destination, index++) = (byte)value;
        }

        public static OperationStatus TryDecode(
            ReadOnlySpan<byte> source, out VarLong result, out int bytesConsumed)
        {
            result = 0;
            bytesConsumed = 0;
            ulong value = 0;
            ulong b;
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

            result = (VarLong)value;
            return OperationStatus.Done;
        }

        public static OperationStatus TryDecode(Stream stream, out VarLong result, out int bytesConsumed)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            result = default;
            bytesConsumed = 0;
            ulong count = 0;
            ulong b;
            do
            {
                if (bytesConsumed == MaxEncodedSize)
                {
                    return OperationStatus.InvalidData;
                }
                else
                {
                    b = (ulong)stream.ReadByte();
                    if (b == ulong.MaxValue)
                        return OperationStatus.NeedMoreData;
                }

                count |= (b & 0x7F) << (bytesConsumed * 7);
                bytesConsumed++;
            }
            while ((b & 0x80) != 0);

            result = (VarLong)count;
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

        public static explicit operator VarLong(ulong value) => new((long)value);

        public static implicit operator long(VarLong value) => value.Value;
        public static implicit operator VarLong(long value) => new(value);
    }
}
