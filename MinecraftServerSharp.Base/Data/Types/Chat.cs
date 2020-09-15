using System.Text.Json;

namespace MinecraftServerSharp
{
    public readonly struct Chat
    {
        public Utf8String Value { get; }

        public Chat(Utf8String value)
        {
            Value = value;
        }

        public static Chat Text(string text)
        {
            var serialized = JsonSerializer.Serialize(new { text });
            return new Chat(new Utf8String(serialized));
        }
    }
}
