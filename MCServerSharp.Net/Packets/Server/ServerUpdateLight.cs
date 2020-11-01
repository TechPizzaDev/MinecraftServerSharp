using System;
using System.Collections.Generic;
using System.Numerics;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateLight)]
    public readonly struct ServerUpdateLight
    {
        [DataProperty(0)] public VarInt ChunkX { get; }
        [DataProperty(1)] public VarInt ChunkY { get; }
        [DataProperty(2)] public bool TrustEdges { get; }
        [DataProperty(3)] public VarInt SkyLightMask { get; }
        [DataProperty(4)] public VarInt BlockLightMask { get; }
        [DataProperty(5)] public VarInt EmptySkyLightMask { get; }
        [DataProperty(6)] public VarInt EmptyBlockLightMask { get; }

        [DataProperty(7)]
        [DataEnumerable]
        public List<LightArray> SkyLightArrays { get; }

        [DataProperty(8)]
        [DataEnumerable]
        public List<LightArray> BlockLightArrays { get; }

        public ServerUpdateLight(
            VarInt chunkX,
            VarInt chunkY, 
            bool trustEdges,
            VarInt skyLightMask, 
            VarInt blockLightMask, 
            VarInt emptySkyLightMask,
            VarInt emptyBlockLightMask,
            List<LightArray> skyLightArrays,
            List<LightArray> blockLightArrays)
        {
            SkyLightArrays = skyLightArrays ?? throw new ArgumentNullException(nameof(skyLightArrays));
            BlockLightArrays = blockLightArrays ?? throw new ArgumentNullException(nameof(blockLightArrays));

            ChunkX = chunkX;
            ChunkY = chunkY;
            TrustEdges = trustEdges;
            SkyLightMask = skyLightMask;
            BlockLightMask = blockLightMask;
            EmptySkyLightMask = emptySkyLightMask;
            EmptyBlockLightMask = emptyBlockLightMask;
        }
    }
    
    [DataObject]
    public class LightArray
    {
        [DataProperty(0)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public byte[] Array { get; }

        public LightArray()
        {
            Array = new byte[2048];
        }
    }
}
