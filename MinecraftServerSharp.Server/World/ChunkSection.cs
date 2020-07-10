using System;

namespace MinecraftServerSharp.World
{
    public class ChunkSection
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int BlockCount = Width * Width * Height;

        public Chunk Parent { get; }

        public int X => Parent.X;
        public int Y => Parent.Z;
        public Dimension Dimension => Parent.Dimension;

        public bool IsEmpty => false;

        public IBlockPalette BlockPalette => DirectBlockPalette.Instance;

        public ChunkSection(Chunk parent)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public BlockState GetState(int x, int y, int z)
        {
            return BlockState.Empty;
        }

        public Block GetBlock(int x, int y, int z)
        {
            return new Block();
        }

        public int GetSkyLight(int x, int y, int z)
        {
            return 15;
        }

        public int GetBlockLight(int x, int y, int z)
        {
            return 15;
        }
    }
}
