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

        public ChunkColumnManager ColumnManager { get; }
        public ChunkColumnPosition Position { get; }

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

        public ValueTask<IChunk> GetOrAddChunk(int chunkY)
        {
            _chunkLock.EnterReadLock();
            try
            {
                if (_chunks.TryGetValue(chunkY, out LocalChunk? chunk))
                    return new ValueTask<IChunk>(chunk);
            }
            finally
            {
                _chunkLock.ExitReadLock();
            }

            _chunkLock.EnterUpgradeableReadLock();
            try
            {
                if (_chunks.TryGetValue(chunkY, out LocalChunk? chunk))
                    return new ValueTask<IChunk>(chunk);

                ChunkPosition position = new(Position, chunkY);

                ValueTask<IChunk> getTask = ColumnManager.ChunkProvider.GetOrAddChunk(ColumnManager, position);
                if (getTask.IsCompleted)
                {
                    LoadChunkContinuation(getTask.Result, this);
                    return getTask;
                }

                Task<IChunk> loadTask = getTask.AsTask().ContinueWith(
                    (t, s) => LoadChunkContinuation(t.Result, s), this, TaskContinuationOptions.ExecuteSynchronously);
                return new ValueTask<IChunk>(loadTask);
            }
            finally
            {
                _chunkLock.ExitUpgradeableReadLock();
            }
        }

        private static IChunk LoadChunkContinuation(IChunk chunk, object? state)
        {
            LocalChunkColumn column = (LocalChunkColumn)state!;
            LocalChunk localChunk = (LocalChunk)chunk;

            column._chunkLock.EnterWriteLock();
            try
            {
                column._chunks.Add(localChunk.Y, localChunk);
                return localChunk;
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
    }
}
