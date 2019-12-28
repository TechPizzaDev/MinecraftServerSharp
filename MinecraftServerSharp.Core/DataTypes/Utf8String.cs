using System;
using System.Text;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct Utf8String
    {
        public static UTF8Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly string _value;

        public Utf8String(string value)
        {
            _value = value;
        }

        public Utf8String(ReadOnlySpan<byte> bytes)
        {
            _value = Encoding.GetString(bytes);
        }

        /// <summary>
        /// Returns the value of this <see cref="Utf8String"/> as a <see cref="string"/>.
        /// </summary>
        public override string ToString() => _value;
    }
}
