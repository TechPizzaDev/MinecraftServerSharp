using System;

namespace MinecraftServerSharp.DataTypes
{
    public readonly struct Chat
    {
        public string Value { get; }

        public Chat(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
