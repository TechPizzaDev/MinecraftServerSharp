
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PlayDisconnect)]
    public readonly struct ServerPlayDisconnect
    {
        [PacketProperty(0)] public Chat Reason { get; }

        public ServerPlayDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
