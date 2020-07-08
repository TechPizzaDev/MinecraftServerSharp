using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace MinecraftServerSharp
{
    [DebuggerDisplay("{Value, nq}")]
    public readonly struct VarLong
    {
        public const int MaxEncodedSize = 10;
        
        public long Value { get; }

        public VarLong(long value)
        {
            Value = value;
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

            } while ((b & 0x80) != 0);

            result = (VarLong)count;
            return OperationStatus.Done;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator long(VarLong value) => value.Value;
        public static explicit operator VarLong(long value) => new VarLong(value);
    }
}
