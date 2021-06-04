using System;
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

        private BitArray32 _blocks;

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

        public byte[] SkyLight;
        public byte[] BlockLight;

        // TODO: dont allow public constructor, use a chunk manager/provider instead

        public LocalChunk(IChunkColumn parent, int y, IBlockPalette blockPalette, BlockState airBlock)
        {
            // TODO: validate Y through a chunk manager or something
            Y = y;

            Column = parent ?? throw new ArgumentNullException(nameof(parent));
            BlockPalette = blockPalette ?? throw new ArgumentNullException(nameof(blockPalette));
            AirBlock = airBlock;

            // TODO: pool storage arrays
            _blocks = BitArray32.AllocateUninitialized(BlockCount, BlockPalette.BitsPerBlock);
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

        /// <summary>
        /// </summary>
        /// <param name="startIndex">The block index to start copying from.</param>
        /// <param name="destination">The block IDs from this <see cref="BlockPalette"/>.</returns>
        /// <returns>The amount of block IDs copied from the chunk.</returns>
        public int GetBlockId(int startIndex, Span<uint> destination)
        {
            return _blocks.Get((uint)startIndex, destination);
        }

        /// <summary>
        /// </summary>
        /// <param name="index">The block index.</param>
        /// <returns>The block ID from this <see cref="BlockPalette"/> at the given block index.</returns>
        public uint GetBlockId(int index)
        {
            return _blocks[index];
        }

        /// <summary>
        /// </summary>
        /// <param name="x">The direct X coordinate.</param>
        /// <param name="y">The direct Y coordinate.</param>
        /// <param name="z">The direct Z coordinate.</param>
        /// <returns>The block ID from this <see cref="BlockPalette"/> at the given block coordinates.</returns>
        public uint GetBlockId(int x, int y, int z)
        {
            int index = GetBlockIndex(x, y, z);
            return GetBlockId(index);
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

        /// <summary>
        /// </summary>
        /// <param name="paletteId">The block ID from this <see cref="BlockPalette"/>.</param>
        /// <param name="index">The block index.</param>
        public void SetBlock(uint paletteId, int index)
        {
            _blocks[index] = paletteId;
            IsEmpty = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="paletteId">The block ID from this <see cref="BlockPalette"/>.</param>
        /// <param name="x">The direct X coordinate.</param>
        /// <param name="y">The direct Y coordinate.</param>
        /// <param name="z">The direct Z coordinate.</param>
        public void SetBlock(uint paletteId, int x, int y, int z)
        {
            int index = GetBlockIndex(x, y, z);
            SetBlock(paletteId, index);
        }

        /// <summary>
        /// </summary>
        /// <param name="paletteIds">The block IDs from this <see cref="BlockPalette"/>.</param>
        /// <param name="startIndex">The block index to start writing to.</param>
        public void SetBlocks(Span<uint> paletteIds, int startIndex)
        {
            _blocks.Set((uint)startIndex, paletteIds);
            IsEmpty = false;
        }

        public void FillBlock(BlockState block)
        {
            _blocks.Fill(BlockPalette.IdForBlock(block));

            if (block == AirBlock)
                IsEmpty = true;
            else
                IsEmpty = false;
        }

        public void FillBlockLevel(BlockState block, int y)
        {
            _blocks.Slice(y * LevelBlockCount, LevelBlockCount).Fill(BlockPalette.IdForBlock(block));
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
