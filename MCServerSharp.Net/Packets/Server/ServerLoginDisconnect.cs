
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.LoginDisconnect)]
    public readonly struct ServerLoginDisconnect
    {
        [DataProperty(0)] public Chat Reason { get; }

        public ServerLoginDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
