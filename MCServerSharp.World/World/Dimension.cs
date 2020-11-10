using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        private Chunk _templateChunk;

        private LongDictionary<ChunkPosition, Chunk> _chunks;
        private Dictionary<Chunk, ChunkInfo> _chunkInfos;

        public List<Player> players = new List<Player>();

        public bool HasSkylight => true;

        public Dimension(DirectBlockPalette directBlockPalette)
        {
            _directBlockPalette = directBlockPalette ?? throw new ArgumentNullException(nameof(directBlockPalette));

            _chunks = new LongDictionary<ChunkPosition, Chunk>();
            _chunkInfos = new Dictionary<Chunk, ChunkInfo>();



            var air = _directBlockPalette.blockLookup["minecraft:air"].DefaultState;
            _templateChunk = new Chunk(this, new ChunkPosition(), air, directBlockPalette);

            var section0 = _templateChunk.Sections.Span[0];

            int y = 0;
            section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:bedrock"].DefaultState, y++);

            for (int j = 0; j < 3; j++)
                section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:dirt"].DefaultState, y++);

            section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:grass_block"].DefaultState, y++);
        }

        // TODO: create actual world/chunk-manager
        List<Chunk> chunksToRemove = new List<Chunk>();

        public void Tick()
        {
            foreach (var (chunk, info) in _chunkInfos)
            {
                info.Age++;

                if (info.Age > 200)
                {
                    chunksToRemove.Add(chunk);
                }
            }

            foreach (var chunk in chunksToRemove)
            {
                _chunks.Remove(chunk.Position);
                _chunkInfos.Remove(chunk);
            }

            foreach (var player in players)
            {
                player.Components.Tick();
            }
        }

        public Chunk GetChunk(ChunkPosition position)
        {
            if (!_chunks.TryGetValue(position, out var chunk))
            {
                //var air = _directBlockPalette.blockLookup["minecraft:air"].DefaultState;

                chunk = new Chunk(this, position, _directBlockPalette);
                chunk._sections = _templateChunk._sections;

                _chunkInfos.Add(chunk, new ChunkInfo());

                var section0 = chunk.Sections.Span[0];

                //int y = 0;
                //section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:bedrock"].DefaultState, y++);
                //
                //for (int j = 0; j < 3; j++)
                //    section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:dirt"].DefaultState, y++);
                //
                //section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:grass_block"].DefaultState, y++);


                _chunks.Add(position, chunk);
            }

            var chunkInfo = _chunkInfos[chunk];
            chunkInfo.Age = 0;
            return chunk;
        }

        public Chunk GetChunk(int x, int z)
        {
            return GetChunk(new ChunkPosition(x, z));
        }
    }
}
