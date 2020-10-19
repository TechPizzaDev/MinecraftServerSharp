
using System.Collections.Generic;
using MCServerSharp.Maths;

namespace MCServerSharp.Entity.Mob
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

        public HashSet<(int, int)> SentChunks = new HashSet<(int, int)>();
    }
}
