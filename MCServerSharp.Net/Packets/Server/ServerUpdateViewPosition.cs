
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateViewPosition)]
    public readonly struct ServerUpdateViewPosition
    {
        [PacketProperty(0)] public VarInt ChunkX { get; }
        [PacketProperty(1)] public VarInt ChunkZ { get; }

        public ServerUpdateViewPosition(VarInt chunkX, VarInt chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }
    }
}
