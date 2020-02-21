
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.Ping)]
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
