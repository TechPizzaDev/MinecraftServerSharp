using System;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct Chat
    {
        public Utf8String Value { get; }

        public Chat(Utf8String value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
