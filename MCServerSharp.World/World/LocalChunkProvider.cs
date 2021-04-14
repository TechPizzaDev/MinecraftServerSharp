using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;
using MCServerSharp.NBT;

namespace MCServerSharp.World
{
    public class LocalChunkProvider : IChunkProvider
    {
        private ReaderWriterLockSlim _chunkLock;
        private Dictionary<ChunkPosition, Task<LocalChunk>> _loadingChunks;

        //private FastNoiseLite _noise;

        public LocalChunkColumnProvider ColumnProvider { get; }

        IChunkColumnProvider IChunkProvider.ColumnProvider => ColumnProvider;

        public LocalChunkProvider(LocalChunkColumnProvider columnProvider)
        {
            ColumnProvider = columnProvider ?? throw new ArgumentNullException(nameof(columnProvider));

            _chunkLock = new();
            _loadingChunks = new();

            //_noise = new FastNoiseLite();
            //_noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            //_noise.SetRotationType3D(FastNoiseLite.RotationType3D.None);
            //_noise.SetFractalType(FastNoiseLite.FractalType.None);
            //_noise.SetFractalOctaves(1);
            //_noise.SetFractalLacunarity(2f);
            //_noise.SetFractalGain(0.5f);
            //_noise.SetFrequency(0.01f);
        }

        private static Dictionary<int, List<BlockState[]>> hhehh = new Dictionary<int, List<BlockState[]>>();

        public async ValueTask<IChunk> GetOrAddChunk(ChunkColumnManager columnManager, ChunkPosition chunkPosition)
        {
            IChunkColumn column = await ColumnProvider.GetOrAddChunkColumn(columnManager, chunkPosition.Column).Unchain();
            if (column.TryGetChunk(chunkPosition.Y, out IChunk? loadedChunk))
            {
                return loadedChunk;
            }

            if (column is not LocalChunkColumn localColumn)
                throw new InvalidOperationException();

            var chunksToDecode = localColumn._chunksToDecode;
            if (chunksToDecode != null)
            {
                NbtElement chunkElement;
                bool hasElement = false;

                lock (chunksToDecode)
                {
                    hasElement = chunksToDecode.Remove(chunkPosition.Y, out chunkElement);
                }

                if (hasElement)
                {
                    // TODO: move to a Anvil chunk parser

                    LocalChunk chunk;

                    if (chunkElement.TryGetCompoundElement("BlockStates", out NbtElement blockStatesNbt) &&
                        chunkElement.TryGetCompoundElement("Palette", out NbtElement paletteNbt))
                    {
                        IndirectBlockPalette palette = ParsePalette(columnManager.GlobalBlockPalette, paletteNbt);
                        chunk = new LocalChunk(column, chunkPosition.Y, palette, columnManager.Air);

                        if (palette.Count == 1 &&
                            palette.BlockForId(0) == columnManager.Air)
                        {
                            chunk.FillBlock(columnManager.Air);
                        }
                        else
                        {
                            ReadOnlyMemory<byte> blockStateRawData = blockStatesNbt.GetArrayData(out NbtType dataType);
                            if (dataType != NbtType.LongArray)
                                throw new InvalidDataException();

                            SetBlocksFromData(chunk, palette, MemoryMarshal.Cast<byte, ulong>(blockStateRawData.Span));
                        }
                    }
                    else
                    {
                        chunk = new LocalChunk(column, chunkPosition.Y, columnManager.GlobalBlockPalette, columnManager.Air);
                        chunk.FillBlock(chunk.AirBlock);
                    }

                    lock (chunksToDecode)
                    {
                        localColumn._chunksToDecodeRefCount--;
                        if (localColumn._chunksToDecodeRefCount == 0)
                        {
                            // TODO: fix
                            //localColumn._encodedColumn?.Dispose();
                            localColumn._encodedColumn = null;
                        }
                    }

                    return chunk;
                }
            }
            return await GenerateChunk(column, chunkPosition.Y).Unchain();
        }

        private static void SetBlocksFromData(LocalChunk destination, IndirectBlockPalette palette, ReadOnlySpan<ulong> blockStateData)
        {
            int bitsPerBlock = Math.Max(4, palette.BitsPerBlock);

            int blocksPerLong = 64 / bitsPerBlock;
            int bitsPerLong = blocksPerLong * bitsPerBlock;
            uint mask = ~(uint.MaxValue << bitsPerBlock);

            int blockNumber = 0;
            for (; blockNumber + blocksPerLong <= 4096;)
            {
                int startLong = blockNumber / blocksPerLong;
                ulong data = blockStateData[startLong];
                if (BitConverter.IsLittleEndian)
                    data = BinaryPrimitives.ReverseEndianness(data);

                for (int i = 0; i < blocksPerLong; i++, blockNumber++)
                {
                    int startOffset = i * bitsPerBlock;
                    uint block = (uint)(data >> startOffset) & mask;

                    destination.SetBlock(block, blockNumber);
                }
            }

            for (; blockNumber < 4096; blockNumber++)
            {
                int startLong = blockNumber / blocksPerLong;
                ulong data = BinaryPrimitives.ReverseEndianness(blockStateData[startLong]);

                int startOffset = (blockNumber * bitsPerBlock) % bitsPerLong;
                uint block = (uint)(data >> startOffset) & mask;

                destination.SetBlock(block, blockNumber);
            }
        }

        private static Utf8String _paletteNameKey = "Name".ToUtf8String();
        private static Utf8String _palettePropertiesKey = "Properties".ToUtf8String();

        private IndirectBlockPalette ParsePalette(DirectBlockPalette globalPalette, NbtElement element)
        {
            ArrayPool<StatePropertyValue> propPool = ArrayPool<StatePropertyValue>.Shared;
            ArrayPool<char> utf16Pool = ArrayPool<char>.Shared;

            StatePropertyValue[] propertyBuffer = propPool.Rent(8);
            char[] utf16Buffer = utf16Pool.Rent(32);
            try
            {
                int blockStateIndex = 0;
                BlockState[] blockStates = new BlockState[element.GetLength()];

                ReadOnlyMemory<char> StoreUtf8(Utf8Memory memory)
                {
                    ReadOnlySpan<byte> utf8 = memory.Span;
                    Span<char> utf16 = utf16Buffer;
                    int totalWritten = 0;

                    do
                    {
                        OperationStatus status = Utf8.ToUtf16(utf8, utf16, out int read, out int written);
                        utf8 = utf8[read..];
                        totalWritten += written;

                        if (status == OperationStatus.DestinationTooSmall)
                        {
                            utf16Pool.Resize(ref utf16Buffer, utf16Buffer.Length * 2);
                            utf16 = utf16Buffer.AsSpan()[totalWritten..];
                        }
                    }
                    while (utf8.Length > 0);

                    return utf16Buffer.AsMemory(0, totalWritten);
                }

                foreach (NbtElement blockStateNbt in element.EnumerateContainer())
                {
                    Utf8Memory name = blockStateNbt[_paletteNameKey].GetUtf8Memory();
                    BlockDescription blockDescription = globalPalette[name];
                    BlockState state = blockDescription.DefaultState;

                    if (blockStateNbt.TryGetCompoundElement(_palettePropertiesKey, out NbtElement propertiesNbt))
                    {
                        ReadOnlySpan<IStateProperty> blockProps = blockDescription.Properties.Span;

                        if (propertyBuffer.Length < blockProps.Length)
                        {
                            int newLength = Math.Max(blockProps.Length, propertyBuffer.Length * 2);
                            propPool.ReturnRent(ref propertyBuffer, newLength);
                        }

                        int propIndex = 0;
                        foreach (IStateProperty prop in blockProps)
                        {
                            if (propertiesNbt.TryGetCompoundElement(prop.Name, out NbtElement propNbt))
                            {
                                ReadOnlyMemory<char> indexName = StoreUtf8(propNbt.GetUtf8Memory());
                                propertyBuffer[propIndex++] = prop.GetPropertyValue(indexName);
                            }
                        }
                        state = blockDescription.GetMatchingState(propertyBuffer.AsSpan(0, propIndex));
                    }

                    blockStates[blockStateIndex++] = state;
                }

                return IndirectBlockPalette.CreateUnsafe(blockStates);
            }
            finally
            {
                propPool.Return(propertyBuffer);
                utf16Pool.Return(utf16Buffer);
            }
        }

        public bool TryGetChunk(ChunkPosition chunkPosition, [MaybeNullWhen(false)] out IChunk chunk)
        {
            if (ColumnProvider.TryGetChunkColumn(chunkPosition.Column, out IChunkColumn? column))
            {
                return column.TryGetChunk(chunkPosition.Y, out chunk);
            }
            chunk = default;
            return false;
        }

        private async Task<LocalChunk> GenerateChunk(IChunkColumn chunkColumn, int chunkY)
        {
            if (chunkColumn is not LocalChunkColumn column)
                throw new ArgumentException(null, nameof(chunkColumn));

            ChunkColumnManager manager = column.ColumnManager;

            //var palette = IndirectBlockPalette.Create(
            //    Enumerable.Range(1384, 16)
            //    .Select(x => manager.GlobalBlockPalette.BlockForId((uint)x))
            //    .Append(manager.Air));

            var palette = manager.GlobalBlockPalette;

            LocalChunk chunk = new LocalChunk(column, chunkY, palette, manager.Air);
            chunk.FillBlock(chunk.AirBlock);

            // TODO: move chunk gen somewhere

            if (false && chunkY == 0)
            {
                //chunk.SetBlock(GlobalBlockPalette.BlockForId(1384), 0, 0, 0);
                //chunk.SetBlock(GlobalBlockPalette.BlockForId(1384), 1, 1, 1);
                //chunk.SetBlock(GlobalBlockPalette.BlockForId(1384), 2, 2, 2);

                //for (int y = 0; y < 16; y++)
                //{
                //    uint id = (uint)(y + 1384);
                //    BlockState block = manager.GlobalBlockPalette.BlockForId(id);
                //
                //    for (int z = 0; z < 16; z++)
                //    {
                //        for (int x = 0; x < 16; x++)
                //        {
                //            int bx = x + chunk.X * 16;
                //            int by = y + chunk.Y * 16 + 16;
                //            int bz = z + chunk.Z * 16;
                //
                //            float n = _noise.GetNoise(bx, by, bz);
                //
                //            n += 1;
                //
                //            if (n > 1f)
                //            {
                //                chunk.SetBlock(block, x, y, z);
                //            }
                //        }
                //    }
                //}

                uint x = (uint)chunk.X % 16;
                uint z = (uint)chunk.Z % 16;
                uint xz = x + z;
                for (uint y = 0; y < 16; y++)
                {
                    // 1384 = wool
                    // 6851 = terracotta

                    uint id = (xz + y) % 16 + 1384;
                    BlockState block = manager.GlobalBlockPalette.BlockForId(id);
                    chunk.FillBlockLevel(block, (int)y);
                }
            }
            return chunk;
        }
    }
}
