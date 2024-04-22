
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SpawnPosition)]
    public readonly struct ServerSpawnPosition
    {
        [DataProperty(0)] public Position Location { get; }
        [DataProperty(1)] public float Angle { get; }

        public ServerSpawnPosition(Position location, float angle)
        {
            Location = location;
            Angle = angle;
        }
    }
}
