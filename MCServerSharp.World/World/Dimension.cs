using MCServerSharp.Blocks;
using MCServerSharp.Collections;

namespace MCServerSharp.World
{
    public class Dimension : ITickable
    {
        private DirectBlockPalette _directBlockPalette;

        private LongDictionary<long, Chunk> _chunks;

        public bool HasSkylight => true;

        public Dimension(DirectBlockPalette directBlockPalette)
        {
            _directBlockPalette = directBlockPalette ?? throw new System.ArgumentNullException(nameof(directBlockPalette));

            _chunks = new LongDictionary<long, Chunk>();
        }

        public void Tick()
        {

        }

        public static long GetChunkKey(int x, int z)
        {
            return (long)x << 32 | (long)z;
        }

        public Chunk GetChunk(int x, int z)
        {
            long key = GetChunkKey(x, z);
            if (!_chunks.TryGetValue(key, out var chunk))
            {
                var air = _directBlockPalette.blockLookup["minecraft:air"].DefaultState;

                chunk = new Chunk(x, z, this, air, _directBlockPalette);

                var section0 = chunk.Sections.Span[0];

                int y = 0;
                section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:bedrock"].DefaultState, y++);

                for (int j = 0; j < 3; j++)
                    section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:dirt"].DefaultState, y++);

                section0.FillLevelBlock(_directBlockPalette.blockLookup["minecraft:grass_block"].DefaultState, y++);


                _chunks.Add(key, chunk);
            }

            return chunk;
        }
    }
}
