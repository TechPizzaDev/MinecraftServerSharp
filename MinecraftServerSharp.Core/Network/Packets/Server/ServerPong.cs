
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ServerPacketID.Pong)]
    public readonly struct ServerPong
    {
        [PacketProperty(0)] public long Payload { get; }

        public ServerPong(long payload)
        {
            Payload = payload;
        }
    }
}
