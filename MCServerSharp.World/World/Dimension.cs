using System;
using System.Collections.Generic;
using MCServerSharp.Blocks;
using MCServerSharp.Collections;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class ChunkInfo
    {
        public int Age;
    }

    public class Dimension : ITickable
    {
        private DirectBlockPalette _directBlockPalette;

        private LongDictionary<long, Chunk> _chunks;
        private Dictionary<Chunk, ChunkInfo> _chunkInfos;

        public List<Player> players = new List<Player>();

        public bool HasSkylight => true;

        public Dimension(DirectBlockPalette directBlockPalette)
        {
            _directBlockPalette = directBlockPalette ?? throw new ArgumentNullException(nameof(directBlockPalette));

            _chunks = new LongDictionary<long, Chunk>();
            _chunkInfos = new Dictionary<Chunk, ChunkInfo>();
        }

        public void Tick()
        {
            var toRemove = new List<Chunk>();
            foreach (var (chunk, info) in _chunkInfos)
            {
                info.Age++;

                if (info.Age > 200)
                {
                    toRemove.Add(chunk);
                }
            }

            foreach (var chunk in toRemove)
            {
                _chunks.Remove(GetChunkKey(chunk));
                _chunkInfos.Remove(chunk);


            }

            foreach(var player in players)
            {
                player.Components.Tick();
            }
        }

        public static long GetChunkKey(int x, int z)
        {
            return (long)x << 32 | (long)z;
        }

        public static long GetChunkKey(Chunk chunk)
        {
            return GetChunkKey(chunk.X, chunk.Z);
        }

        public Chunk GetChunk(int x, int z)
        {
            long key = GetChunkKey(x, z);
            if (!_chunks.TryGetValue(key, out var chunk))
            {
                var air = _directBlockPalette.blockLookup["minecraft:air"].DefaultState;

                chunk = new Chunk(x, z, this, air, _directBlockPalette);
                _chunkInfos.Add(chunk, new ChunkInfo());

                var section0 = chunk.Sections.Span[0];

                int y = 0;
                section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:bedrock"].DefaultState, y++);

                for (int j = 0; j < 3; j++)
                    section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:dirt"].DefaultState, y++);

                section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:grass_block"].DefaultState, y++);


                _chunks.Add(key, chunk);
            }

            var chunkInfo = _chunkInfos[chunk];
            chunkInfo.Age = 0;
            return chunk;
        }

        public Chunk GetChunk(ChunkPosition position)
        {
            return GetChunk(position.X, position.Z);
        }
    }
}
