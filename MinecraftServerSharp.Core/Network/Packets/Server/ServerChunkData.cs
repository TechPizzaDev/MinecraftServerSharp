using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    // TODO: create flash-copying (fast deep-clone) of chunks for serialization purposes

    [PacketStruct(ServerPacketId.ChunkData)]
    public readonly struct ServerChunkData
    {
        public int ChunkX { get; }
        public int ChunkY { get; }
        public bool FullChunk { get; }
        public VarInt PrimaryBitMask { get; }
        public 
    }


}
