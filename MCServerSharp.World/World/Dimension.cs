using System;
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

    public class Dimension : ComponentEntity, ITickable 
    {
        public ChunkColumnManager ChunkColumnManager { get; }
        public DirectBlockPalette GlobalBlockPalette => ChunkColumnManager.GlobalBlockPalette;

        // TODO: turn players into proper API
        public List<Player> players = new();

        public bool HasSkylight => true;

        public Dimension(ChunkColumnManager chunkColumnManager)
        {
            ChunkColumnManager = chunkColumnManager ?? throw new ArgumentNullException(nameof(chunkColumnManager));
        }

        List<BasicChunkColumn> chunksToRemove = new List<BasicChunkColumn>();

        public void Tick()
        {
            chunksToRemove.Clear();

            foreach (var (chunk, info) in _chunkInfos)
            {
                info.Age++;

                if (info.Age > 200)
                {
                    chunksToRemove.Add(chunk);
                }
            }

            foreach (var chunk in chunksToRemove)
            {
                _chunkColumns.Remove(chunk.Position);
                _chunkInfos.Remove(chunk);
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
