
namespace MinecraftServerSharp.Network.Packets
{
    public readonly struct ServerDisconnect
    {
        [PacketProperty(0)] public Chat Reason { get; }

        public ServerDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
