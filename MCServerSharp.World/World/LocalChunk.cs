﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public partial class LocalChunk : ComponentEntity, IChunk
    {
        public const int Width = 16;
        public const int Height = 16;
        public const int LevelBlockCount = Width * Width;
        public const int BlockCount = LevelBlockCount * Height;

        // TODO: compress based on palette
        private uint[] _blocks;

        public IChunkColumn Column { get; }
        public int Y { get; }

        // TODO: add dynamic palette (that gets trimmed on serialize) and compressed block storage 
        public IBlockPalette BlockPalette { get; }
        public BlockState AirBlock { get; }

        // TODO: make this dynamic based on a dynamic block palette 
        public bool IsEmpty { get; private set; }

        public int X => Column.Position.X;
        public int Z => Column.Position.Z;
        public ChunkPosition Position => new ChunkPosition(X, Y, Z);

        public Dimension Dimension => this.GetComponent<DimensionComponent>().Dimension;

        // TODO: dont allow public constructor, use a chunk manager/provider instead

        public LocalChunk(IChunkColumn parent, int y, IBlockPalette blockPalette, BlockState airBlock)
        {
            // TODO: validate Y through a chunk manager or something
            Y = y;

            Column = parent ?? throw new ArgumentNullException(nameof(parent));
            BlockPalette = blockPalette ?? throw new ArgumentNullException(nameof(blockPalette));
            AirBlock = airBlock;

            _blocks = GC.AllocateUninitializedArray<uint>(BlockCount, false);
            FillBlock(airBlock);
        }

        public BlockEnumerator EnumerateBlocks()
        {
            return new BlockEnumerator(this);
        }

        /// <summary>
        /// Returns an index to a block array.
        /// </summary>
        /// <remarks>
        /// Blocks and block states are stored in YZX order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBlockIndex(int x, int y, int z)
        {
            Debug.Assert((uint)x < 16u);
            Debug.Assert((uint)y < 16u);
            Debug.Assert((uint)z < 16u);
            return (y * Width + z) * Width + x;
        }

        public BlockState GetBlock(int index)
        {
            return BlockPalette.BlockForId(_blocks[index]);
        }

        public BlockState GetBlock(int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            return GetBlock(blockIndex);
        }

        public void SetBlock(BlockState block, int index)
        {
            _blocks[index] = BlockPalette.IdForBlock(block);
            IsEmpty = false;
        }

        public void SetBlock(BlockState block, int x, int y, int z)
        {
            int blockIndex = GetBlockIndex(x, y, z);
            SetBlock(block, blockIndex);
        }

        public void FillBlock(BlockState block)
        {
            _blocks.AsSpan().Fill(BlockPalette.IdForBlock(block));

            if (block == AirBlock)
                IsEmpty = true;
            else
                IsEmpty = false;
        }

        public void FillBlockLevel(BlockState block, int y)
        {
            _blocks.AsSpan(y * LevelBlockCount, LevelBlockCount).Fill(BlockPalette.IdForBlock(block));
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

        public ChunkCommandList CreateCommandList()
        {
            throw new NotImplementedException();
        }

        public ValueTask SubmitCommands(ChunkCommandList commandList)
        {
            throw new NotImplementedException();
        }
    }
}