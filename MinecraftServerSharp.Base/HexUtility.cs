using System;
using System.Globalization;

namespace MCServerSharp
{
    public static class HexUtility
    {
        // TODO: replace hex methods with fast net5 ones

        public static int GetHexCharCount(int byteCount)
        {
            return byteCount * 2;
        }

        public static void ToHexString(ReadOnlySpan<byte> source, Span<char> destination)
        {
            destination = destination.Slice(0, Math.Min(destination.Length, GetHexCharCount(source.Length)));
            source = source.Slice(0, Math.Min(source.Length, destination.Length / 2));

            if (source.IsEmpty || destination.IsEmpty)
                return;

            for (int i = 0; i < destination.Length / 2; i++)
            {
                int value = source[i];

                int a = value >> 4;
                destination[i * 2 + 0] = (char)(a > 9 ? a + 0x37 : a + 0x30);

                int b = value & 0xF;
                destination[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
        }

        /// <summary>
        /// Create a hex string from an array of bytes
        /// </summary>
        public static unsafe string ToHexString(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
                return string.Empty;

            // TODO: pass Span directly, not now as ref-structs are not supported by generics

            fixed (byte* srcPtr = source)
            {
                return string.Create(source.Length * 2, (IntPtr)srcPtr, (dst, srcPtr) =>
                {
                    var src = new ReadOnlySpan<byte>((byte*)srcPtr, dst.Length / 2);
                    ToHexString(src, dst);
                });
            }
        }

        public static int GetHexByteCount(int textLength)
        {
            return (textLength + 1) / 2;
        }

        /// <summary>
        /// Convert a hexadecimal text to into bytes.
        /// </summary>
        public static void FromHexString(ReadOnlySpan<char> source, Span<byte> destination)
        {
            destination = destination.Slice(0, Math.Min(destination.Length, GetHexByteCount(source.Length)));
            source = source.Slice(0, Math.Min(source.Length / 2 * 2, destination.Length / 2));

            if (source.IsEmpty || destination.IsEmpty)
                return;

            int i = 0;
            for (; i < source.Length; i += 2)
                destination[i / 2] = byte.Parse(source[i..(i + 1)], NumberStyles.HexNumber);

            if (source.Length - i > 0)
                destination[i] = byte.Parse(stackalloc char[] { source[i] }, NumberStyles.HexNumber);
        }

        public static byte[] FromHexString(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
                return Array.Empty<byte>();

            var array = new byte[GetHexByteCount(text.Length)];
            FromHexString(text, array);
            return array;
        }
    }
}
