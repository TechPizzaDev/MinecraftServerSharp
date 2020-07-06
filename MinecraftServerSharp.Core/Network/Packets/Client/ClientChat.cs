using System;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.ChatMessage)]
    public readonly struct ClientChat
    {
        public string Message { get; }

        [PacketConstructor]
        public ClientChat(
            [LengthConstraint(Max = 256)] string message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
