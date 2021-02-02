using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;

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

        public async ValueTask<IChunk> GetOrAddChunk(ChunkColumnManager columnManager, ChunkPosition chunkPosition)
        {
            IChunkColumn column = await ColumnProvider.GetOrAddChunkColumn(columnManager, chunkPosition.ColumnPosition).Unchain();
            if (column.TryGetChunk(chunkPosition.Y, out IChunk? loadedChunk))
                return loadedChunk;

            return await LoadOrGenerateChunk(column, chunkPosition.Y).Unchain();
        }

        public bool TryGetChunk(ChunkPosition chunkPosition, [MaybeNullWhen(false)] out IChunk chunk)
        {
            if (ColumnProvider.TryGetChunkColumn(chunkPosition.ColumnPosition, out IChunkColumn? column))
            {
                return column.TryGetChunk(chunkPosition.Y, out chunk);
            }
            chunk = default;
            return false;
        }

        private async Task<LocalChunk> LoadOrGenerateChunk(IChunkColumn column, int chunkY)
        {
            ChunkColumnManager manager = column.ColumnManager;
            LocalChunk chunk = new LocalChunk(column, chunkY, manager.GlobalBlockPalette, manager.Air);
            
            // TODO: move chunk gen somewhere

            if (chunkY == 0)
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
