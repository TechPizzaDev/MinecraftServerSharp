using System;
using System.Text;

namespace MinecraftServerSharp.Network
{
    public static class NetTextHelper
    {
        public const int MaxStringLength = 32767;

        public static UTF8Encoding Utf8 { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        public static UnicodeEncoding BigUtf16 { get; } = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
        public static UnicodeEncoding LittleUtf16 { get; } = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);

        public static bool IsValidStringLength(int length)
        {
            return length >= 1 && length <= MaxStringLength;
        }

        public static bool IsValidStringByteLength(int byteLength)
        {
            return byteLength >= 1 && byteLength <= MaxStringLength * 4;
        }

        public static void AssertValidStringLength(int length)
        {
            if (IsValidStringLength(length))
                throw new ArgumentException(nameof(length));
        }

        public static void AssertValidStringByteLength(int byteLength)
        {
            if (IsValidStringByteLength(byteLength))
                throw new ArgumentOutOfRangeException(nameof(byteLength));
        }
    }
}
