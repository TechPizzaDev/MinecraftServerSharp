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
        public int Age;
    }

    public class Dimension : ComponentEntity, ITickable
    {
        private ConcurrentDictionary<IChunkColumn, ChunkInfo> _chunkInfos = new();

        public ChunkColumnManager ChunkColumnManager { get; }

        public DirectBlockPalette GlobalBlockPalette => ChunkColumnManager.GlobalBlockPalette;

        // TODO: turn players into proper API
        public List<Player> players = new();

        public bool HasSkylight => true;

        public Dimension(ChunkColumnManager chunkColumnManager)
        {
            ChunkColumnManager = chunkColumnManager ?? throw new ArgumentNullException(nameof(chunkColumnManager));

            ChunkColumnManager.ChunkColumnProvider.ChunkAdded += ChunkColumnProvider_ChunkAdded;
        }

        private void ChunkColumnProvider_ChunkAdded(IChunkColumnProvider arg1, IChunkColumn arg2)
        {
            _chunkInfos.TryAdd(arg2, new ChunkInfo());
        }

        List<IChunkColumn> chunksToRemove = new List<IChunkColumn>();

        public void Tick()
        {
            chunksToRemove.Clear();

            // TODO: chunk reference counter 
            foreach ((IChunkColumn chunk, ChunkInfo info) in _chunkInfos)
            {
                info.Age++;

                if (info.Age > 200)
                {
                    chunksToRemove.Add(chunk);
                }
            }

            void TryRemove(IChunkColumn? chunk)
            {
                if (chunk != null)
                    _chunkInfos.TryRemove(chunk, out _);
            }

            foreach (IChunkColumn? chunk in chunksToRemove)
            {
                ValueTask<IChunkColumn?> removeTask = ChunkColumnManager.ChunkColumnProvider.RemoveChunkColumn(chunk.Position);
                if (removeTask.IsCompleted)
                    TryRemove(removeTask.Result);
                else
                    removeTask.AsTask().ContinueWith((c) => TryRemove(c.Result));
            }

            foreach (var player in players)
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
