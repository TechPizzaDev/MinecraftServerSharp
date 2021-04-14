
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.ChatMessage)]
    public readonly struct ServerChat
    {
        [DataProperty(0)] public Chat JsonData { get; }
        [DataProperty(1)] public byte Position { get; }
        [DataProperty(2)] public UUID Sender { get; }

        public ServerChat(Chat jsonData, byte position, UUID sender)
        {
            JsonData = jsonData;
            Position = position;
            Sender = sender;
        }
    }
}
