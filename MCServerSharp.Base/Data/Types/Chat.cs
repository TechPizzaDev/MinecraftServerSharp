using System.Text.Json;

namespace MCServerSharp
{
    public readonly struct Chat
    {
        public Utf8String? Value { get; }

        public Chat(Utf8String? value)
        {
            Value = value;
        }

        public static Chat Text(string text)
        {
            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(new { text });
            return new Chat(Utf8String.WrapUnsafe(serialized));
        }
    }
}
