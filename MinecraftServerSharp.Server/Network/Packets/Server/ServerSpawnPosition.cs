
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SpawnPosition)]
    public readonly struct ServerSpawnPosition
    {
        [PacketProperty(0)] public Position Location { get; }

        public ServerSpawnPosition(Position location)
        {
            Location = location;
        }
    }
}
