using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Packets
{
    public readonly struct ServerDisconnect
    {
        [PacketProperty(0)] public Chat Reason { get; }

        [PacketConstructor]
        public ServerDisconnect(Chat reason)
        {
            Reason = reason;
        }
    }
}
