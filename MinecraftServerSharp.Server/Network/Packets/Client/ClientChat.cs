using System;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.ChatMessage)]
    public readonly struct ClientChat
    {
        public Utf8String Message { get; }

        [PacketConstructor]
        public ClientChat(
            [LengthConstraint(Max = 256)] Utf8String message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
