using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class Chunk : ComponentEntity
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int LevelBlockCount = Width * Width;
        public const int BlockCount = LevelBlockCount * Height;

        // TODO: compress based on palette
        private BlockState[] _blocks;

        public IChunkColumn Parent { get; }
        public int Y { get; }

        // TODO: add dynamic palette (that gets trimmed on serialize) and compressed block storage 
        public IBlockPalette BlockPalette { get; }
        public BlockState AirBlock { get; }

        // TODO: make this dynamic based on a dynamic block palette 
        public bool IsEmpty { get; private set; }

        public int X => Parent.Position.X;
        public int Z => Parent.Position.Z;
        public ChunkPosition Position => new ChunkPosition(X, Y, Z);

        public Dimension Dimension => this.GetComponent<DimensionComponent>().Dimension;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Blocks are stored in YZX order.
        /// </remarks>
        public ReadOnlyMemory<BlockState> Blocks => _blocks;

        // TODO: dont allow public constructor, use a chunk manager/provider instead

        public Chunk(IChunkColumn parent, int y, BlockState airBlock, IBlockPalette blockPalette)
        {
            // TODO: validate Y through a chunk manager or something
            Y = y;

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

        public void FillBlockLevel(BlockState block, int y)
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
