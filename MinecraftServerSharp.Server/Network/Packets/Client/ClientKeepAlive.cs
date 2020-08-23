
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.KeepAlive)]
    public readonly struct ClientKeepAlive
    {
        [PacketProperty(0)] public long KeepAliveId { get; }

        [PacketConstructor]
        public ClientKeepAlive(long keepAliveId)
        {
            KeepAliveId = keepAliveId;
        }
    }
}
