using System.Text;

namespace MinecraftServerSharp.Network
{
    public static class NetTextHelper
    {
        public static UTF8Encoding Utf8 { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        public static UnicodeEncoding BigUtf16 { get; } = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
        public static UnicodeEncoding LittleUtf16 { get; } = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);

    }
}
