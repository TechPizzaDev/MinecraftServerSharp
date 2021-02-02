using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;

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

        public static OperationStatus TryDecode(Stream stream, out VarLong result, out int bytes)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            bytes = 0;
            long count = 0;
            long b;
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

        public static implicit operator long(VarLong value) => value.Value;
        public static explicit operator VarLong(long value) => new VarLong(value);
    }
}
