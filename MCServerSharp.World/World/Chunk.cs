using System;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class Chunk
    {
        public const int SectionCount = 16;
        public const int Height = SectionCount * ChunkSection.Height;
        public const int BlockCount = SectionCount * ChunkSection.BlockCount;

        public ChunkSection?[] _sections;

        public ChunkSection? this[int y] => _sections[y];

        public ChunkPosition Position { get; }
        public Dimension Dimension { get; }

        // TODO: allow infinite™ amount of chunk sections

        public int X => Position.X;
        public int Z => Position.Z;
        public ReadOnlyMemory<ChunkSection?> Sections => _sections;

        // TODO: fix this funky constructor mess (needs redesign)

        public Chunk(Dimension dimension, ChunkPosition position)
        {
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));
            Position = position;
        }

        public Chunk(Dimension dimension, ChunkPosition position, BlockState airBlock, IBlockPalette blockPalette)
        {
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));
            Position = position;

            _sections = new ChunkSection?[SectionCount];
            for (int y = 0; y < _sections.Length; y++)
            {
                _sections[y] = new ChunkSection(this, y, airBlock, blockPalette);
            }
        }

        public int GetBiome(int x, int y, int z)
        {
            return 127; // VOID
        }

        public int GetSectionMask()
        {
            int sectionMask = 0;
            for (int sectionY = 0; sectionY < _sections.Length; sectionY++)
            {
                var section = _sections[sectionY];
                if (section != null)
                    sectionMask |= 1 << sectionY; // Set that bit to true in the mask
            }
            return sectionMask;
        }
    }
}
