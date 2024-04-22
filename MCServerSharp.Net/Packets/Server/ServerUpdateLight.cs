using System;
using System.Buffers;
using System.Collections.Generic;
using MCServerSharp.Collections;

namespace MCServerSharp.Net.Packets
{
    [DataObject]
    public readonly struct LightUpdate : IDisposable
    {
        [DataProperty(0)] public bool TrustEdges { get; }
        [DataProperty(1)] public BitSet SkyLightMask { get; }
        [DataProperty(2)] public BitSet BlockLightMask { get; }
        [DataProperty(3)] public BitSet EmptySkyLightMask { get; }
        [DataProperty(4)] public BitSet EmptyBlockLightMask { get; }

        [DataProperty(5)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public List<LightArray> SkyLightArrays { get; }

        [DataProperty(6)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public List<LightArray> BlockLightArrays { get; }

        public ArrayPool<byte>? Pool { get; }

        public LightUpdate(
            bool trustEdges,
            BitSet skyLightMask,
            BitSet blockLightMask,
            BitSet emptySkyLightMask,
            BitSet emptyBlockLightMask,
            List<LightArray> skyLightArrays,
            List<LightArray> blockLightArrays,
            ArrayPool<byte>? pool)
        {
            SkyLightArrays = skyLightArrays ?? throw new ArgumentNullException(nameof(skyLightArrays));
            BlockLightArrays = blockLightArrays ?? throw new ArgumentNullException(nameof(blockLightArrays));
            SkyLightMask = skyLightMask ?? throw new ArgumentNullException(nameof(skyLightMask));
            BlockLightMask = blockLightMask ?? throw new ArgumentNullException(nameof(blockLightMask));
            EmptySkyLightMask = emptySkyLightMask ?? throw new ArgumentNullException(nameof(emptySkyLightMask));
            EmptyBlockLightMask = emptyBlockLightMask ?? throw new ArgumentNullException(nameof(emptyBlockLightMask));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));

            TrustEdges = trustEdges;
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

    [PacketStruct(ServerPacketId.UpdateLight)]
    public readonly struct ServerUpdateLight
    {
        [DataProperty(0)] public VarInt ChunkX { get; }
        [DataProperty(1)] public VarInt ChunkY { get; }
        [DataProperty(2)] public LightUpdate Light { get; }

        public ServerUpdateLight(
            VarInt chunkX,
            VarInt chunkY,
            LightUpdate light)
        {
            ChunkX = chunkX;
            ChunkY = chunkY;
            Light = light;
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
