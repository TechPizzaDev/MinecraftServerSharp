
namespace MinecraftServerSharp.Network.Packets
{

    [PacketStruct(ServerPacketId.KeepAlive)]
    public readonly struct ServerKeepAlive
    {
        [PacketProperty(0)] public long KeepAliveId { get; }

        public ServerKeepAlive(long keepAliveId)
        {
            KeepAliveId = keepAliveId;
        }
    }
}
