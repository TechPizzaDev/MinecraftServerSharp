using System;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public class Chunk
    {
        public const int SectionCount = 16;
        public const int Height = SectionCount * ChunkSection.Height;
        public const int BlockCount = SectionCount * ChunkSection.BlockCount;

        private ChunkSection[] _sections;

        public ChunkSection this[int y] => _sections[y];

        public int X { get; }
        public int Z { get; }
        public Dimension Dimension { get; }

        public ReadOnlyMemory<ChunkSection> Sections => _sections;

        public Chunk(int x, int z, Dimension dimension, IBlockPalette blockPalette)
        {
            X = x;
            Z = z;
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));

            _sections = new ChunkSection[SectionCount];
            for (int y = 0; y < _sections.Length; y++)
            {
                _sections[y] = new ChunkSection(this, y, blockPalette);
            }
        }

        public int GetBiome(int x, int y, int z)
        {
            return 127; // VOID
        }
    }
}
