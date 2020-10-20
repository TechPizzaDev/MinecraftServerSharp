
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.Pong)]
    public readonly struct ServerPong
    {
        [DataProperty(0)] public long Payload { get; }

        public ServerPong(long payload)
        {
            Payload = payload;
        }
    }
}
