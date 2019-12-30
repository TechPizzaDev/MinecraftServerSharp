using System;
using MinecraftServerSharp.Network;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct Utf8String
    {
        private readonly string _value;

        public Utf8String(string value)
        {
            _value = value;
        }

        public Utf8String(ReadOnlySpan<byte> bytes)
        {
            _value = NetTextHelper.Utf8.GetString(bytes);
        }

        /// <summary>
        /// Returns the value of this <see cref="Utf8String"/> as a <see cref="string"/>.
        /// </summary>
        public override string ToString() => _value;
    }
}
