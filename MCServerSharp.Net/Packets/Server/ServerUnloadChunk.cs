
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UnloadChunk)]
    public readonly struct ServerUnloadChunk
    {
        [DataProperty(0)] public int ChunkX { get; }
        [DataProperty(1)] public int ChunkZ { get; }

        public ServerUnloadChunk(int chunkX, int chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }
    }
}
