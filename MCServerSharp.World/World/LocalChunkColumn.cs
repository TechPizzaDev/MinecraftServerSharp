using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;
using MCServerSharp.NBT;

namespace MCServerSharp.World
{
    public class LocalChunkColumn : IChunkColumn
    {
        private ReaderWriterLockSlim _chunkLock = new();
        private Dictionary<int, LocalChunk> _chunks = new(24 + 2); // 24 chunks + 2 empty light chunks

        internal NbtDocument? _encodedColumn;
        internal Dictionary<int, NbtElement>? _chunksToDecode;
        internal int _chunksToDecodeRefCount;

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
        }

        public int GetMaxChunkCount()
        {
            Dimension dimension = Dimension;
            int diff = dimension.Height - dimension.MinY;
            int representedCount = (diff + LocalChunk.Height - 1) / LocalChunk.Height;
            return representedCount + 2;
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

            return AddChunk(chunkY);
        }

        private ValueTask<IChunk> AddChunk(int chunkY)
        {
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
            _chunkLock.EnterReadLock();
            try
            {
                return _chunks.TryGetValue(chunkY, out chunk);
            }
            finally
            {
                _chunkLock.ExitReadLock();
            }
        }

        bool IChunkColumn.TryGetChunk(int chunkY, [MaybeNullWhen(false)] out IChunk chunk)
        {
            bool result = TryGetChunk(chunkY, out LocalChunk? lchunk);
            chunk = lchunk;
            return result;
        }

        public void TryGetChunks(Span<IChunk?> chunks)
        {
            _chunkLock.EnterReadLock();
            try
            {
                int count = GetMaxChunkCount();
                if (chunks.Length > count)
                {
                    chunks = chunks.Slice(0, count);
                }

                int offset = Dimension.MinY / LocalChunk.Height;

                for (int i = 0; i < chunks.Length; i++)
                {
                    int y = i + offset;
                    _chunks.TryGetValue(y, out LocalChunk? lchunk);
                    chunks[i] = lchunk;
                }
            }
            finally
            {
                _chunkLock.ExitReadLock();
            }
        }

        [SuppressMessage("Reliability", "CA2012", Justification = "Performance")]
        public void GetOrAddChunks(Span<ValueTask<IChunk>> chunks)
        {
            int count = GetMaxChunkCount();
            if (chunks.Length > count)
            {
                chunks = chunks.Slice(0, count);
            }

            int offset = Dimension.MinY / LocalChunk.Height;

            for (int i = 0; i < chunks.Length; i++)
            {
                int y = i + offset;
                chunks[i] = GetOrAddChunk(y);
            }
        }
    }
}
