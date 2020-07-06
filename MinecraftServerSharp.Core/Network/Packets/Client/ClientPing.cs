
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.Ping)]
    public readonly struct ClientPing
    {
        public long Payload { get; }

        [PacketConstructor]
        public ClientPing(long payload)
        {
            Payload = payload;
        }
    }
}
