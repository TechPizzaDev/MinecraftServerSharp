using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    // TODO: create proper world/chunk-manager
    // this system would also be distributed (possibly over different processes or even machines)

    // TODO?: DimensionTaskScheduler: allows async tasks to run while the dimension thread continues to tick

    class ChunkInfo
    {
        public IChunkColumn Column;
        public int Age;
    }

    public class Dimension : ComponentEntity, ITickable
    {
        private ConcurrentQueue<IChunkColumn> _infoAddQueue = new ConcurrentQueue<IChunkColumn>();
        private ConcurrentQueue<IChunkColumn> _infoRemoveQueue = new ConcurrentQueue<IChunkColumn>();
        private List<ChunkInfo> _chunkInfos = new();
        private List<IChunkColumn> _chunksToRemove = new();

        // TODO: turn players into proper API
        public List<Player> players = new();

        public ChunkColumnManager ChunkColumnManager { get; }

        public DirectBlockPalette GlobalBlockPalette => ChunkColumnManager.GlobalBlockPalette;

        public bool HasSkylight => true;

        public Dimension(ChunkColumnManager chunkColumnManager)
        {
            ChunkColumnManager = chunkColumnManager ?? throw new ArgumentNullException(nameof(chunkColumnManager));

            ChunkColumnManager.ChunkColumnProvider.ChunkAdded += ChunkColumnProvider_ChunkAdded;
        }

        private void ChunkColumnProvider_ChunkAdded(IChunkColumnProvider arg1, IChunkColumn arg2)
        {
            _infoAddQueue.Enqueue(arg2);
        }

        public void Tick()
        {
            while (_infoAddQueue.TryDequeue(out IChunkColumn? addColumn))
                _chunkInfos.Add(new ChunkInfo() { Column = addColumn });

            while (_infoRemoveQueue.TryDequeue(out IChunkColumn? removeColumn))
            {
                int index = _chunkInfos.FindIndex((c) => c.Column == removeColumn);
                if (index == -1)
                {
                    _infoRemoveQueue.Enqueue(removeColumn);
                    continue;
                }

                //Console.WriteLine("removed " + _chunkInfos[index].Column.Position + " - " + _chunkInfos.Count + " left");
                _chunkInfos.RemoveAt(index);
            }

            _chunksToRemove.Clear();

            // TODO: chunk reference counter 
            foreach (ChunkInfo info in _chunkInfos)
            {
                info.Age++;

                if (info.Age > 200)
                {
                    _chunksToRemove.Add(info.Column);
                }
            }

            void TryRemove(IChunkColumn? chunk)
            {
                if (chunk != null)
                    _infoRemoveQueue.Enqueue(chunk);
            }

            foreach (IChunkColumn? chunk in _chunksToRemove)
            {
                ValueTask<IChunkColumn?> removeTask = ChunkColumnManager.ChunkColumnProvider.RemoveChunkColumn(chunk.Position);
                if (removeTask.IsCompleted)
                    TryRemove(removeTask.Result);
                else
                    removeTask.AsTask().ContinueWith((c) => TryRemove(c.Result));
            }

            foreach (Player? player in players)
            {
                player.Components.Tick();
            }
        }

        public ValueTask<IChunkColumn> GetOrAddChunkColumn(ChunkColumnPosition position)
        {
            return ChunkColumnManager.GetOrAddChunkColumn(position);
        }

        public ValueTask<IChunkColumn> GetOrAddChunkColumn(int x, int z)
        {
            return GetOrAddChunkColumn(new ChunkColumnPosition(x, z));
        }
    }
}
