using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public class ChunkSection
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int LevelBlockCount = Width * Width;
        public const int BlockCount = LevelBlockCount * Height;

        private BlockState[] _blocks;

        public Chunk Parent { get; }
        public int SectionY { get; }

        // TODO: add dynamic palette that gets trimmed on serialize?
        public IBlockPalette BlockPalette { get; }
        public BlockState AirBlock { get; }

        public int X => Parent.X;
        public int Z => Parent.Z;
        public Dimension Dimension => Parent.Dimension;

        public bool IsEmpty { get; private set; }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Blocks are stored in YZX order.
        /// </remarks>
        public ReadOnlyMemory<BlockState> Blocks => _blocks;

        public ChunkSection(Chunk parent, int sectionY, BlockState airBlock, IBlockPalette blockPalette)
        {
            if (sectionY < 0 || sectionY > Chunk.SectionCount)
                throw new ArgumentOutOfRangeException(nameof(sectionY));
            SectionY = sectionY;

            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            AirBlock = airBlock ?? throw new ArgumentNullException(nameof(airBlock));
            BlockPalette = blockPalette ?? throw new ArgumentNullException(nameof(blockPalette));

            _blocks = new BlockState[BlockCount];
            FillBlock(airBlock);
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
            return _blocks[index];
        }

        public BlockState GetBlock(int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            return GetBlock(blockIndex);
        }

        public void SetBlock(BlockState block, int index)
        {
            _blocks[index] = block ?? throw new ArgumentNullException(nameof(block));
            IsEmpty = false;
        }

        public void SetBlock(BlockState block, int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            SetBlock(block, blockIndex);
        }

        public void FillBlock(BlockState block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            _blocks.AsSpan().Fill(block);

            if (block == AirBlock)
                IsEmpty = true;
            else
                IsEmpty = false;
        }

        public void FillLevelBlock(BlockState block, int y)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            _blocks.AsSpan(y * LevelBlockCount, LevelBlockCount).Fill(block);
            IsEmpty = false;
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
