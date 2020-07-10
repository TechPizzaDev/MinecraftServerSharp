using System;

namespace MinecraftServerSharp.World
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

        public Chunk(int x, int y, Dimension dimension)
        {
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));

            _sections = new ChunkSection[SectionCount];
            for (int i = 0; i < _sections.Length; i++)
                _sections[i] = new ChunkSection(this);
        }

        public int GetBiome(int x, int y, int z)
        {
            return 127; // VOID
        }
    }
}
