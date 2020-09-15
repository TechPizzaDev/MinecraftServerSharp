
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.ChatMessage)]
    public readonly struct ServerChat
    {
        [PacketProperty(0)] public Chat JsonData { get; }
        [PacketProperty(1)] public byte Position { get; }

        public ServerChat(Chat jsonData, byte position)
        {
            JsonData = jsonData;
            Position = position;
        }
    }
}
