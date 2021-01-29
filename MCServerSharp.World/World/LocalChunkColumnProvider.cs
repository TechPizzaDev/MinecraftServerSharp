using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class LocalChunkColumnProvider : IChunkColumnProvider
    {
        private ReaderWriterLockSlim _columnLock;
        private Dictionary<ChunkColumnPosition, IChunkColumn> _columns;
        private Dictionary<ChunkColumnPosition, Task<IChunkColumn>> _loadingColumns;
        private Dictionary<ChunkColumnPosition, Task<IChunkColumn?>> _unloadingColumns;

        public event Action<IChunkColumnProvider, IChunkColumn>? ChunkAdded;
        public event Action<IChunkColumnProvider, IChunkColumn>? ChunkRemoved;

        public LocalChunkColumnProvider()
        {
            _columnLock = new();
            _columns = new();
            _loadingColumns = new();
            _unloadingColumns = new();
        }

        private bool TryRemoveColumn(ChunkColumnPosition columnPosition, out ValueTask<IChunkColumn?> task)
        {
            if (_unloadingColumns.TryGetValue(columnPosition, out Task<IChunkColumn?>? unloadTask))
            {
                task = new ValueTask<IChunkColumn?>(unloadTask);
                return true;
            }

            if (!_columns.ContainsKey(columnPosition) &&
                !_loadingColumns.ContainsKey(columnPosition))
            {
                task = default;
                return true;
            }

            task = default;
            return false;
        }

        // TODO: unload cancellation
        public ValueTask<IChunkColumn?> RemoveChunkColumn(ChunkColumnPosition columnPosition)
        {
            _columnLock.EnterReadLock();
            try
            {
                if (TryRemoveColumn(columnPosition, out ValueTask<IChunkColumn?> task))
                    return task;
            }
            finally
            {
                _columnLock.ExitReadLock();
            }

            _columnLock.EnterUpgradeableReadLock();
            try
            {
                if (TryRemoveColumn(columnPosition, out ValueTask<IChunkColumn?> task))
                    return task;

                Task<IChunkColumn?> unloadTask = UnloadColumn(columnPosition)
                    .ContinueWith((finishedTask) =>
                {
                    _columnLock.EnterWriteLock();
                    try
                    {
                        _columns.Remove(columnPosition);
                        _unloadingColumns.Remove(columnPosition);

                        IChunkColumn? result = finishedTask.Result;
                        if (result != null)
                            ChunkRemoved?.Invoke(this, result);
                        return result;
                    }
                    finally
                    {
                        _columnLock.ExitWriteLock();
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                if (!unloadTask.IsCompleted)
                {
                    _columnLock.EnterWriteLock();
                    try
                    {
                        _unloadingColumns.Add(columnPosition, unloadTask);
                    }
                    finally
                    {
                        _columnLock.ExitWriteLock();
                    }
                }
                return new ValueTask<IChunkColumn?>(unloadTask);
            }
            finally
            {
                _columnLock.ExitUpgradeableReadLock();
            }
        }

        public ValueTask<ChunkStatus> GetChunkColumnStatus(ChunkColumnPosition columnPosition)
        {
            _columnLock.EnterReadLock();
            try
            {
                if (_columns.ContainsKey(columnPosition))
                    return new ValueTask<ChunkStatus>(ChunkStatus.Loaded);

                if (_loadingColumns.ContainsKey(columnPosition))
                    return new ValueTask<ChunkStatus>(ChunkStatus.Queued);
            }
            finally
            {
                _columnLock.ExitReadLock();
            }
            return new ValueTask<ChunkStatus>(ChunkStatus.Unloaded);
        }

        public ValueTask<IChunkColumn> GetOrAddChunkColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            _columnLock.EnterReadLock();
            try
            {
                if (TryGetColumn(columnPosition, out ValueTask<IChunkColumn> task))
                    return task;
            }
            finally
            {
                _columnLock.ExitReadLock();
            }

            // Only one thread can enter an upgradeable lock.
            _columnLock.EnterUpgradeableReadLock();
            try
            {
                if (TryGetColumn(columnPosition, out ValueTask<IChunkColumn> task))
                    return task;

                Task<IChunkColumn> loadTask = LoadOrGenerateColumn(columnManager, columnPosition)
                    .ContinueWith((finishedTask) =>
                {
                    _columnLock.EnterWriteLock();
                    try
                    {
                        IChunkColumn? result = finishedTask.Result;
                        _columns.Add(columnPosition, result);
                        _loadingColumns.Remove(columnPosition);
                        ChunkAdded?.Invoke(this, result);
                        return result;
                    }
                    finally
                    {
                        _columnLock.ExitWriteLock();
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                if (!loadTask.IsCompleted)
                {
                    _columnLock.EnterWriteLock();
                    try
                    {
                        _loadingColumns.Add(columnPosition, loadTask);
                    }
                    finally
                    {
                        _columnLock.ExitWriteLock();
                    }
                }
                return new ValueTask<IChunkColumn>(loadTask);
            }
            finally
            {
                _columnLock.ExitUpgradeableReadLock();
            }
        }

        private bool TryGetColumn(ChunkColumnPosition columnPosition, out ValueTask<IChunkColumn> task)
        {
            if (_columns.TryGetValue(columnPosition, out IChunkColumn? column))
            {
                task = new ValueTask<IChunkColumn>(column);
                return true;
            }

            if (_loadingColumns.TryGetValue(columnPosition, out Task<IChunkColumn>? loadTask))
            {
                task = new ValueTask<IChunkColumn>(loadTask);
                return true;
            }

            task = default;
            return false;
        }

        public bool TryGetChunkColumn(ChunkColumnPosition columnPosition, [MaybeNullWhen(false)] out IChunkColumn chunkColumn)
        {
            _columnLock.EnterReadLock();
            try
            {
                if (_columns.TryGetValue(columnPosition, out IChunkColumn? column))
                {
                    chunkColumn = column;
                    return true;
                }
                chunkColumn = default;
                return false;
            }
            finally
            {
                _columnLock.ExitReadLock();
            }
        }

        private async Task<IChunkColumn> LoadOrGenerateColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            IChunkColumn? column = await LoadColumn(columnManager, columnPosition).Unchain();
            if (column != null)
                return column;

            return await GenerateColumn(columnManager, columnPosition).Unchain();
        }

        private ValueTask<IChunkColumn?> LoadColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            //string filePath = "";
            //
            //if (!File.Exists(filePath))
            //    return null;
            //
            //int bufferSize = 4096;
            //
            //await using FileStream chunkFile = new FileStream(
            //    filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.Asynchronous);

            return default;
        }

        private async Task<IChunkColumn> GenerateColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            return new LocalChunkColumn(columnManager, columnPosition);
        }

        private async Task<IChunkColumn?> UnloadColumn(ChunkColumnPosition columnPosition)
        {
            if (!_columns.TryGetValue(columnPosition, out IChunkColumn? column))
            {
                if (_loadingColumns.TryGetValue(columnPosition, out Task<IChunkColumn>? loadTask))
                    column = await loadTask;
            }

            if (column != null)
            {
                // TODO: unload
            }

            return column;
        }
    }
}
