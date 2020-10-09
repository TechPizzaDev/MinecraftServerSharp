using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MCServerSharp.World
{
    public class ChunkSection
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int BlockCount = Width * Width * Height;

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
        /// Block states are stored in YZX order.
        /// </remarks>
        public ReadOnlyMemory<BlockState> Blocks => _blockStates;

        public ChunkSection(Chunk parent, int sectionY, IBlockPalette blockPalette)
        {
            if (sectionY < 0 || sectionY > Chunk.SectionCount)
                throw new ArgumentOutOfRangeException(nameof(sectionY));
            SectionY = sectionY;

            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            BlockPalette = blockPalette ?? throw new ArgumentNullException(nameof(blockPalette));

            _blockStates = new BlockState[BlockCount];

            FillState(BlockState.Empty);
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
            Debug.Assert((uint)x < 16);
            Debug.Assert((uint)y < 16);
            Debug.Assert((uint)z < 16);
            return x + Width * (y + Width * z);
        }

        public BlockState GetBlock(int index)
        {
            return _blockStates[index];
        }

        public BlockState GetBlock(int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            return GetBlock(blockIndex);
        }

        public void SetBlock(BlockState block, int index)
        {
            _blockStates[index] = block ?? throw new ArgumentNullException(nameof(block));
        }

        public void SetBlock(BlockState block, int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            SetBlock(block, blockIndex);
        }

        public void FillState(BlockState block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            _blockStates.AsSpan().Fill(block);
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
