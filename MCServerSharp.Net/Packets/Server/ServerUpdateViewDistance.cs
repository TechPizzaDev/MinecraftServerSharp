
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateViewDistance)]
    public readonly struct ServerUpdateViewDistance
    {
        [PacketProperty(0)] public VarInt ViewDistance { get; }

        public ServerUpdateViewDistance(VarInt viewDistance)
        {
            ViewDistance = viewDistance;
        }
    }
}
