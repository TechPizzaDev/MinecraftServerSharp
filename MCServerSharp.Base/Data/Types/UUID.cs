using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MCServerSharp.Utility;

namespace MCServerSharp
{
    // TODO: add parsing

    public readonly struct UUID : IEquatable<UUID>, ILongHashable
    {
        public const int MaxHexStringLength = 8 + 4 + 4 + 4 + 12;
        public const int MaxHyphenHexStringLength = 4 + MaxHexStringLength;
        public const int MaxIntArrayStringLength = 51;
        public const int MaxStringLength = 51;

        public static UUID Zero => default;

        public ulong X { get; }
        public ulong Y { get; }

        public ReadOnlySpan<int> IntArray => MemoryMarshal.Cast<UUID, int>(UnsafeR.AsReadOnlySpan(this));

        public bool IsRfc4122
        {
            get
            {
                // TODO:
                throw new NotImplementedException();
            }
        }

        public UUID(ulong x, ulong y)
        {
            X = x;
            Y = y;
        }

        // TODO:
        //public static UUID CreateRfc4122()
        //{
        //
        //}

        public bool Equals(UUID other)
        {
            return X == other.X
                && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is UUID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return ToString(UUIDRepresentation.HyphenHex, false);
        }

        public Utf8String ToUtf8String()
        {
            return (Utf8String)ToString(UUIDRepresentation.HyphenHex, false);
        }

        public string ToString(UUIDRepresentation representation, bool compact)
        {
            Span<char> tmp = stackalloc char[MaxStringLength];
            if (!TryFormat(tmp, out int charsWritten, representation, compact))
                throw new Exception();

            return new string(tmp.Slice(0, charsWritten));
        }

        public Utf8String ToUtf8String(UUIDRepresentation representation, bool compact)
        {
            Span<char> tmp = stackalloc char[MaxStringLength];
            if (!TryFormat(tmp, out int charsWritten, representation, compact))
                throw new Exception();

            return new Utf8String(tmp.Slice(0, charsWritten));
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            UUIDRepresentation representation = UUIDRepresentation.HyphenHex,
            bool compact = false)
        {
            // The hyphen are set to split the UUID into numbers of the format 8-4-4-4-12 with 
            // each number marking the number of hexadecimal digits fitting into the corresponding section.

            // 00000001-0002-0003-0004-000000000005 == 1-2-3-4-5
            // 00000001000200030004000000000005 == 1000200030004000000000005

            if (representation == UUIDRepresentation.IntArray)
            {
                static bool WriteInt(int value, Span<char> dst, ref int offset)
                {
                    bool result = value.TryFormat(dst.Slice(offset), out int len);
                    offset += len;
                    return result;
                }

                charsWritten = 0;
                if (destination.Length < 3)
                    return false;

                destination[charsWritten++] = '[';
                destination[charsWritten++] = 'I';
                destination[charsWritten++] = ';';

                var intArray = IntArray;
                for (int i = 0; i < intArray.Length; i++)
                {
                    if (!WriteInt(intArray[i], destination, ref charsWritten))
                        return false;
                }

                if (destination.Length < 1)
                    return false;

                destination[charsWritten] = ']';
                return true;
            }

            static void TrimStart(ref Span<char> src)
            {
                src = src.TrimStart('0');
            }

            static void CopyTo(Span<char> src, Span<char> dst, ref int offset)
            {
                src.CopyTo(dst.Slice(offset));
                offset += src.Length;
            }

            Span<byte> data = stackalloc byte[sizeof(ulong) * 2];
            BinaryPrimitives.WriteUInt64LittleEndian(data, X);
            BinaryPrimitives.WriteUInt64LittleEndian(data.Slice(sizeof(ulong)), Y);

            Span<char> p = stackalloc char[MaxHexStringLength];
            HexUtility.ToHexString(data, p);
            Span<char> p1 = p[0..7];
            Span<char> p2 = p[7..11];
            Span<char> p3 = p[11..15];
            Span<char> p4 = p[15..19];
            Span<char> p5 = p[19..31];

            switch (representation)
            {
                case UUIDRepresentation.HyphenHex:
                    if (compact)
                    {
                        TrimStart(ref p1);
                        TrimStart(ref p2);
                        TrimStart(ref p3);
                        TrimStart(ref p4);
                        TrimStart(ref p5);
                    }

                    charsWritten = 0;
                    if (destination.Length < 4 + p1.Length + p2.Length + p3.Length + p4.Length + p5.Length)
                        return false;

                    CopyTo(p1, destination, ref charsWritten);
                    destination[charsWritten++] = '-';
                    CopyTo(p2, destination, ref charsWritten);
                    destination[charsWritten++] = '-';
                    CopyTo(p3, destination, ref charsWritten);
                    destination[charsWritten++] = '-';
                    CopyTo(p4, destination, ref charsWritten);
                    destination[charsWritten++] = '-';
                    CopyTo(p5, destination, ref charsWritten);
                    return true;

                case UUIDRepresentation.Hex:
                    if (compact)
                    {
                        TrimStart(ref p1);
                    }

                    charsWritten = 0;
                    if (destination.Length < p1.Length + p2.Length + p3.Length + p4.Length + p5.Length)
                        return false;

                    CopyTo(p1, destination, ref charsWritten);
                    CopyTo(p2, destination, ref charsWritten);
                    CopyTo(p3, destination, ref charsWritten);
                    CopyTo(p4, destination, ref charsWritten);
                    CopyTo(p5, destination, ref charsWritten);
                    return true;

                case UUIDRepresentation.MostLeast:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(representation));
            }
        }

        public static bool operator ==(UUID left, UUID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UUID left, UUID right)
        {
            return !(left == right);
        }
    }

    public enum UUIDRepresentation
    {
        HyphenHex,
        Hex,
        MostLeast,
        IntArray
    }
}
