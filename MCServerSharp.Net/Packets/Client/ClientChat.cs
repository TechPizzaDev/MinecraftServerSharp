using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.ChatMessage)]
    public readonly struct ClientChat
    {
        public Utf8String Message { get; }

        [PacketConstructor]
        public ClientChat(
            [DataLengthConstraint(Max = 256)] Utf8String message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
