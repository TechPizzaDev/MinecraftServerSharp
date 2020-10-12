using MCServerSharp.Blocks;
using MCServerSharp.Collections;

namespace MCServerSharp.World
{
    public class Dimension : ITickable
    {
        private DirectBlockPalette _directBlockPalette;

        private LongDictionary<long, Chunk> _chunks;
        private int i;

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
                chunk = new Chunk(x, z, this, _directBlockPalette);

                foreach (var section in chunk.Sections.Span)
                {
                    var palette = section.BlockPalette;
                    
                    for (int j = 0; j < ChunkSection.BlockCount; j++)
                    {
                        var state = palette.BlockForId((uint)(i++));
                        i = (i + 1) % palette.Count;

                        section.SetBlock(state, j);
                    }
                }

                _chunks.Add(key, chunk);
            }

            return chunk;
        }
    }
}
