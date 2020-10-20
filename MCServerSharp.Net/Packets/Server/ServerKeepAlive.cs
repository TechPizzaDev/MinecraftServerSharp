
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.KeepAlive)]
    public readonly struct ServerKeepAlive
    {
        [DataProperty(0)] public long KeepAliveId { get; }

        public ServerKeepAlive(long keepAliveId)
        {
            KeepAliveId = keepAliveId;
        }
    }
}
