using System;
using System.Collections.Generic;
using MCServerSharp.Maths;
using MCServerSharp.World;

namespace MCServerSharp.Entities.Mobs
{
    public class Player : Mob
    {
        public string? UserName { get; set; }
        public UUID UserUUID { get; set; }

        public ChunkColumnPosition CameraPosition { get; set; }
        public int ViewDistance { get; set; } = 8;

        public Vector3d Position { get; set; }

        public int ChunkX => (int)Math.Floor(Position.X) / 16;
        public int ChunkZ => (int)Math.Floor(Position.Z) / 16;
        public ChunkColumnPosition ChunkPosition => new(ChunkX, ChunkZ);

        public bool ScheduleFullChunkView;
        public HashSet<ChunkColumnPosition> LoadedChunks;
        public HashSet<ChunkColumnPosition> ChunksToUnload;
        public List<List<ChunkColumnPosition>> ChunkLoadLists;

        public HashSet<ChunkColumnPosition> ChunkLoadSet; // TODO: get rid of this

        public long SentColumnCount;

        public Player(Dimension dimension) : base(dimension)
        {
            LoadedChunks = new HashSet<ChunkColumnPosition>();
            ChunksToUnload = new HashSet<ChunkColumnPosition>();
            ChunkLoadLists = new List<List<ChunkColumnPosition>>();

            ChunkLoadSet = new HashSet<ChunkColumnPosition>();
        }
    }
}
