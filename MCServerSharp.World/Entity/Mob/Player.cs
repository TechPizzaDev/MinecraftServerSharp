
using System.Collections.Generic;
using MCServerSharp.Maths;
using MCServerSharp.World;

namespace MCServerSharp.Entities.Mobs
{
    public class Player : Mob
    {
        // TODO: move this to Player class
        public string? UserName { get; set; }

        public ChunkPosition ChunkPosition { get; set; }
        public ChunkPosition LastChunkPosition { get; set; }

        public Vector3d Position { get; set; }
        public Vector3d LastPosition { get; set; }
        public int IntY { get; set; }
        public int LastIntY { get; set; }

        public HashSet<ChunkPosition> SentChunks = new HashSet<ChunkPosition>();
        public List<ChunkPosition> ChunksToSend = new List<ChunkPosition>();
        public HashSet<Player> PlayersInRange = new HashSet<Player>();

        public Player(Dimension dimension) : base(dimension)
        {
        }

        public void UpdateChunksToSend(int viewDistance)
        {
            int xoffset = ChunkPosition.X;
            int zoffset = ChunkPosition.Z;

            ChunksToSend.Clear();
            for (int z = -viewDistance; z < viewDistance; z++)
            {
                for (int x = -viewDistance; x < viewDistance; x++)
                {
                    int cx = x + xoffset;
                    int cz = z + zoffset;

                    ChunksToSend.Add(new ChunkPosition(cx, cz));
                }
            }

            ChunksToSend.Sort(new ChunkPositionDistanceComparer(ChunkPosition));
        }
    }

    public class ChunkPositionDistanceComparer : IComparer<ChunkPosition>
    {
        public ChunkPosition Origin { get; }

        public ChunkPositionDistanceComparer(ChunkPosition origin)
        {
            Origin = origin;
        }

        public int Compare(ChunkPosition x, ChunkPosition y)
        {
            var dx = ChunkPosition.DistanceSquared(x, Origin);
            var dy = ChunkPosition.DistanceSquared(y, Origin);

            return dx.CompareTo(dy);
        }
    }

}
