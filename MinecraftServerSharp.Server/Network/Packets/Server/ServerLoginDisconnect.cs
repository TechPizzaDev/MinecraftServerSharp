
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.LoginDisconnect)]
    public readonly struct ServerLoginDisconnect
    {
        [PacketProperty(0)] public Chat Reason { get; }

        public ServerLoginDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
