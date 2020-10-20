
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateViewPosition)]
    public readonly struct ServerUpdateViewPosition
    {
        [DataProperty(0)] public VarInt ChunkX { get; }
        [DataProperty(1)] public VarInt ChunkZ { get; }

        public ServerUpdateViewPosition(VarInt chunkX, VarInt chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }
    }
}
