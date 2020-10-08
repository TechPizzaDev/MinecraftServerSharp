using System;
using System.Runtime.CompilerServices;

namespace MCServerSharp.World
{
    public class ChunkSection
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int BlockCount = Width * Width * Height;

        private Block[] _blocks;
        private BlockState[] _blockStates;

        public Chunk Parent { get; }
        public int SectionY { get; }
        public IBlockPalette BlockPalette { get; }

        public int X => Parent.X;
        public int Z => Parent.Z;
        public Dimension Dimension => Parent.Dimension;

        public bool IsEmpty => false;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Blocks are stored in YZX order.
        /// </remarks>
        public ReadOnlyMemory<Block> Blocks => _blocks;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Block states are stored in YZX order.
        /// </remarks>
        public ReadOnlyMemory<BlockState> BlockStates => _blockStates;

        public ChunkSection(Chunk parent, int sectionY, IBlockPalette blockPalette)
        {
            if (sectionY < 0 || sectionY > Chunk.SectionCount)
                throw new ArgumentOutOfRangeException(nameof(sectionY));
            SectionY = sectionY;

            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            BlockPalette = blockPalette ?? throw new ArgumentNullException(nameof(blockPalette));

            _blocks = new Block[BlockCount];
            _blockStates = new BlockState[BlockCount];

            _blocks.AsSpan().Fill(new Block(0));
            _blockStates.AsSpan().Fill(BlockState.Empty);
        }

        /// <summary>
        /// Returns an index to a block or block state array.
        /// </summary>
        /// <remarks>
        /// Blocks and block states are stored in YZX order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBlockIndex(int x, int y, int z)
        {
            return x + Width * (y + Width * z);
        }

        public BlockState GetBlockState(int index)
        {
            return _blockStates[index];
        }

        public BlockState GetBlockState(int x, int y, int z)
        {
            return GetBlockState(GetBlockIndex(x, y, z));
        }

        public Block GetBlock(int index)
        {
            return _blocks[index];
        }

        public Block GetBlock(int x, int y, int z)
        {
            return GetBlock(GetBlockIndex(x, y, z));
        }

        public void SetBlock(Block block, int index)
        {
            _blocks[index] = block;
        }

        public void SetBlock(Block block, int x, int y, int z)
        {
            SetBlock(block, GetBlockIndex(x, y, z));
        }

        public void Fill(Block block)
        {
            _blocks.AsSpan().Fill(block);
        }
        
        public void Fill(BlockState blockState)
        {
            _blockStates.AsSpan().Fill(blockState);
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
