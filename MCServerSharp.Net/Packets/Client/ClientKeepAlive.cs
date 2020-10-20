
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.KeepAlive)]
    public readonly struct ClientKeepAlive
    {
        [DataProperty(0)] public long KeepAliveId { get; }

        [PacketConstructor]
        public ClientKeepAlive(long keepAliveId)
        {
            KeepAliveId = keepAliveId;
        }
    }
}
