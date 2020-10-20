
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SpawnPosition)]
    public readonly struct ServerSpawnPosition
    {
        [DataProperty(0)] public Position Location { get; }

        public ServerSpawnPosition(Position location)
        {
            Location = location;
        }
    }
}
