using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Collections;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class LocalChunkColumn : IChunkColumn
    {
        private ReaderWriterLockSlim _chunkLock;
        private Dictionary<int, LocalChunk> _chunks;
        private Dictionary<int, Task<LocalChunk>> _loadingChunks;

        public ChunkColumnManager ColumnManager { get; }
        public ChunkColumnPosition Position { get; }

        public ReadOnlyDictionary<int, LocalChunk> Chunks { get; }

        public IChunkColumnProvider ColumnProvider => ColumnManager.ChunkColumnProvider;
        public DirectBlockPalette GlobalBlockPalette => ColumnManager.GlobalBlockPalette;
        public Dimension Dimension => ColumnManager.Dimension;
        public int X => Position.X;
        public int Z => Position.Z;

        // TODO: fix this funky constructor mess (needs redesign)

        public LocalChunkColumn(ChunkColumnManager columnManager, ChunkColumnPosition position)
        {
            ColumnManager = columnManager ?? throw new ArgumentNullException(nameof(columnManager));
            Position = position;

            _chunkLock = new();
            _chunks = new();
            _loadingChunks = new();

            Chunks = _chunks.AsReadOnlyDictionary();
        }

        public bool ContainsChunk(int chunkY)
        {
            _chunkLock.EnterReadLock();
            try
            {
                return _chunks.ContainsKey(chunkY);
            }
            finally
            {
                _chunkLock.ExitReadLock();
            }
        }

        public ValueTask<LocalChunk> GetOrAddChunk(int chunkY)
        {
            _chunkLock.EnterReadLock();
            try
            {
                if (_chunks.TryGetValue(chunkY, out LocalChunk? chunk))
                    return new ValueTask<LocalChunk>(chunk);
            }
            finally
            {
                _chunkLock.ExitReadLock();
            }

            _chunkLock.EnterUpgradeableReadLock();
            try
            {
                if (_chunks.TryGetValue(chunkY, out LocalChunk? chunk))
                    return new ValueTask<LocalChunk>(chunk);

                Task<LocalChunk> loadTask = LoadOrGenerateChunk(chunkY);
                if (loadTask.IsCompleted)
                {
                    LoadChunkContinuation(loadTask, this);
                    return new ValueTask<LocalChunk>(loadTask);
                } 

                _chunkLock.EnterWriteLock();
                try
                {
                    loadTask = loadTask.ContinueWith(LoadChunkContinuation, this, TaskContinuationOptions.ExecuteSynchronously);
                    _loadingChunks.Add(chunkY, loadTask);
                }
                finally
                {
                    _chunkLock.ExitWriteLock();
                }
                return new ValueTask<LocalChunk>(loadTask);
            }
            finally
            {
                _chunkLock.ExitUpgradeableReadLock();
            }
        }

        private static LocalChunk LoadChunkContinuation(Task<LocalChunk> finishedTask, object? state)
        {
            LocalChunkColumn column = (LocalChunkColumn)state!;
            LocalChunk chunk = finishedTask.Result;

            column._chunkLock.EnterWriteLock();
            try
            {
                column._chunks.Add(chunk.Y, chunk);
                column._loadingChunks.Remove(chunk.Y);
                return chunk;
            }
            finally
            {
                column._chunkLock.ExitWriteLock();
            }
        }

        public bool TryGetChunk(int chunkY, [MaybeNullWhen(false)] out LocalChunk chunk)
        {
            if (_chunks.TryGetValue(chunkY, out LocalChunk? mchunk))
            {
                chunk = mchunk;
                return true;
            }
            chunk = default;
            return false;
        }

        ValueTask<IChunk> IChunkColumn.GetOrAddChunk(int chunkY)
        {
            ValueTask<LocalChunk> task = GetOrAddChunk(chunkY);
            if (task.IsCompleted)
            {
                return new ValueTask<IChunk>(task.Result);
            }
            return new ValueTask<IChunk>(task.AsTask().ContinueWith(x => (IChunk)x));
        }

        bool IChunkColumn.TryGetChunk(int chunkY, [MaybeNullWhen(false)] out IChunk chunk)
        {
            if (_chunks.TryGetValue(chunkY, out LocalChunk? mchunk))
            {
                chunk = mchunk;
                return true;
            }
            chunk = default;
            return false;
        }

        private async Task<LocalChunk> LoadOrGenerateChunk(int chunkY)
        {
            LocalChunk chunk = new LocalChunk(this, chunkY, GlobalBlockPalette, ColumnManager.Air);

            // TODO: move chunk gen somewhere

            if (chunkY == 0)
            {
                uint x = (uint)X % 16;
                uint z = (uint)Z % 16;
                uint xz = x + z;
                for (uint y = 0; y < 16; y++)
                {
                    // 1384 = wool
                    // 6851 = terracotta

                    uint id = (xz + y) % 16 + 1384;
                    BlockState block = GlobalBlockPalette.BlockForId(id);
                    chunk.FillBlockLevel(block, (int)y);
                }
            }

            return chunk;
        }
    }
}
