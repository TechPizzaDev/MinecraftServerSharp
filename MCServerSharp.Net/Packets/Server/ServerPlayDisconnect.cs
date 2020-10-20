
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PlayDisconnect)]
    public readonly struct ServerPlayDisconnect
    {
        [DataProperty(0)] public Chat Reason { get; }

        public ServerPlayDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
