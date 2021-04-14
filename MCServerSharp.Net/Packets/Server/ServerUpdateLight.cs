using System;
using System.Buffers;
using System.Collections.Generic;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateLight)]
    public readonly struct ServerUpdateLight : IDisposable
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

        public ArrayPool<byte>? Pool { get; }

        public ServerUpdateLight(
            VarInt chunkX,
            VarInt chunkY,
            bool trustEdges,
            VarInt skyLightMask,
            VarInt blockLightMask,
            VarInt emptySkyLightMask,
            VarInt emptyBlockLightMask,
            List<LightArray> skyLightArrays,
            List<LightArray> blockLightArrays,
            ArrayPool<byte>? pool)
        {
            SkyLightArrays = skyLightArrays ?? throw new ArgumentNullException(nameof(skyLightArrays));
            BlockLightArrays = blockLightArrays ?? throw new ArgumentNullException(nameof(blockLightArrays));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));

            ChunkX = chunkX;
            ChunkY = chunkY;
            TrustEdges = trustEdges;
            SkyLightMask = skyLightMask;
            BlockLightMask = blockLightMask;
            EmptySkyLightMask = emptySkyLightMask;
            EmptyBlockLightMask = emptyBlockLightMask;
        }

        public void Dispose()
        {
            if (Pool != null)
            {
                foreach (var array in SkyLightArrays)
                    array.Return(Pool);

                foreach (var array in BlockLightArrays)
                    array.Return(Pool);
            }
        }
    }

    [DataObject]
    public unsafe readonly struct LightArray
    {
        public const int Length = 2048;

        private readonly byte[] _data;

        [DataProperty(0)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public Span<byte> Data => _data.AsSpan(0, Length);

        public LightArray(byte[] data)
        {
            _data = data;
        }

        public LightArray(ArrayPool<byte> pool) : this(pool.Rent(Length))
        {
        }

        public void Return(ArrayPool<byte> pool)
        {
            pool.Return(_data);
        }
    }
}
