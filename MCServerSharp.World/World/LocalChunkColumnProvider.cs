using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class LocalChunkColumnProvider : IChunkColumnProvider
    {
        private ReaderWriterLockSlim _columnLock = new();
        private Dictionary<ChunkColumnPosition, IChunkColumn> _columns = new();
        private Dictionary<ChunkColumnPosition, Task<IChunkColumn>> _loadingColumns = new();
        private Dictionary<ChunkColumnPosition, Task<IChunkColumn?>> _unloadingColumns = new();

        public event Action<IChunkColumnProvider, IChunkColumn>? ChunkAdded;
        public event Action<IChunkColumnProvider, IChunkColumn>? ChunkRemoved;

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
                    return new ValueTask<ChunkStatus>(ChunkStatus.InQueue);
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

                Task<IChunkColumn> loadTask = LoadOrGenerateColumn(columnManager, columnPosition);
                Task continuation = loadTask.ContinueWith(static (finishedTask, self) =>
                {
                    var t = Unsafe.As<LocalChunkColumnProvider>(self)!;
                    IChunkColumn result = finishedTask.Result;
                    t._columnLock.EnterWriteLock();
                    try
                    {
                        t._columns.Add(result.Position, result);
                        t._loadingColumns.Remove(result.Position);
                        t.ChunkAdded?.Invoke(t, result);
                    }
                    finally
                    {
                        t._columnLock.ExitWriteLock();
                    }
                }, this, TaskContinuationOptions.ExecuteSynchronously);

                if (!continuation.IsCompleted)
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

        private ReaderWriterLockSlim _regionLock = new();
        private Dictionary<ChunkRegionPosition, IChunkRegion> _regions = new();
        private Dictionary<ChunkRegionPosition, Task<IChunkRegion>> _loadingRegions = new();
        private Dictionary<ChunkRegionPosition, Task<IChunkRegion?>> _unloadingRegions = new();

        private bool TryGetRegion(ChunkRegionPosition regionPosition, out ValueTask<IChunkRegion> task)
        {
            if (_regions.TryGetValue(regionPosition, out IChunkRegion? column))
            {
                task = new ValueTask<IChunkRegion>(column);
                return true;
            }

            if (_loadingRegions.TryGetValue(regionPosition, out Task<IChunkRegion>? loadTask))
            {
                task = new ValueTask<IChunkRegion>(loadTask);
                return true;
            }

            task = default;
            return false;
        }

        private ValueTask<IChunkRegion> GetRegion(ChunkRegionPosition regionPosition)
        {
            _regionLock.EnterReadLock();
            try
            {
                if (TryGetRegion(regionPosition, out ValueTask<IChunkRegion> task))
                    return task;
            }
            finally
            {
                _regionLock.ExitReadLock();
            }

            // Only one thread can enter an upgradeable lock.
            _regionLock.EnterUpgradeableReadLock();
            try
            {
                if (TryGetRegion(regionPosition, out ValueTask<IChunkRegion> task))
                    return task;

                Task<IChunkRegion> loadTask = LoadRegion(regionPosition);
                Task continuation = loadTask.ContinueWith((finishedTask) =>
                {
                    IChunkRegion result = finishedTask.Result;
                    _regionLock.EnterWriteLock();
                    try
                    {
                        _regions.Add(regionPosition, result);
                        _loadingRegions.Remove(regionPosition);
                        //RegionAdded?.Invoke(this, result); // TODO
                    }
                    finally
                    {
                        _regionLock.ExitWriteLock();
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                if (!continuation.IsCompleted)
                {
                    _regionLock.EnterWriteLock();
                    try
                    {
                        _loadingRegions.Add(regionPosition, loadTask);
                    }
                    finally
                    {
                        _regionLock.ExitWriteLock();
                    }
                }
                return new ValueTask<IChunkRegion>(loadTask);
            }
            finally
            {
                _regionLock.ExitUpgradeableReadLock();
            }
        }

        private Task<IChunkRegion> LoadRegion(ChunkRegionPosition regionPosition)
        {
            return Task.Run(() =>
            {
                string fileName = $"r.{regionPosition.X}.{regionPosition.Z}.mca";
                string root = Directory.Exists("region") ? "region" : @"..\..\..\..\MCJarServer\1.16.5\world\region";
                string filePath = Path.Combine(root, fileName);

                if (File.Exists(filePath))
                {
                    int bufferSize = 1024 * 32;

                    FileStream chunkStream = new FileStream(
                        filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize);

                    Console.WriteLine("Loading existing region \"" + fileName + "\"");
                    return new LocalChunkRegion(chunkStream);
                }
                else
                {
                    Console.WriteLine("No file for region \"" + fileName + "\"");
                    return (IChunkRegion)new LocalChunkRegion();
                }
            });
        }

        private async ValueTask<IChunkColumn?> LoadColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition)
        {
            ChunkRegionPosition regionPos = new(columnPosition);
            IChunkRegion region = await GetRegion(regionPos);
            IChunkColumn? column = await region.LoadColumn(columnManager, columnPosition);
            return column;
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

        public IChunkProvider CreateChunkProvider()
        {
            return new LocalChunkProvider(this);
        }
    }
}
