using MCServerSharp.Collections;

namespace MCServerSharp.World
{
    public class Dimension : ITickable
    {
        private LongDictionary<long, Chunk> _chunks;
        private DirectBlockPalette _directPalette;
        private int i = 0;

        public bool HasSkylight => true;

        public Dimension()
        {
            _chunks = new LongDictionary<long, Chunk>();

            _directPalette = new DirectBlockPalette();
            uint num = 100;
            for (uint j = 0; j < num; j++)
            {
                //if (j == 6)
                //    continue;

                var state = new BlockState();
                _directPalette._stateToId.Add(state, j);
                _directPalette._idToState.Add(j, state);
            }
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
                chunk = new Chunk(x, z, this, _directPalette);

                foreach (var section in chunk.Sections.Span)
                {
                    var palette = section.BlockPalette;
                    var state = palette.StateForId((uint)(i++));

                    section.Fill(state);

                    i = (i + 1) % palette.Count;
                }

                _chunks.Add(key, chunk);
            }

            return chunk;
        }
    }
}
