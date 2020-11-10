
using System;
using System.Collections.Generic;
using MCServerSharp.Collections;
using MCServerSharp.Maths;
using MCServerSharp.World;

namespace MCServerSharp.Entities.Mobs
{
    public class Player : Mob
    {
        public string? UserName { get; set; }
        public UUID UserUUID { get; set; }

        public ChunkPosition CameraPosition { get; set; }
        public int ViewDistance { get; set; } = 8;

        public Vector3d Position { get; set; }

        public int ChunkX => (int)Math.Floor(Position.X) / 16;
        public int ChunkZ => (int)Math.Floor(Position.Z) / 16;
        public ChunkPosition ChunkPosition => new ChunkPosition(ChunkX, ChunkZ);

        public bool ScheduleFullChunkView;
        public LongHashSet<ChunkPosition> LoadedChunks;
        public LongHashSet<ChunkPosition> ChunksToUnload;
        public List<List<ChunkPosition>> ChunkLoadLists;

        public LongHashSet<ChunkPosition> ChunkLoadSet; // TODO: get rid of this

        public long SentChunkCount;

        public Player(Dimension dimension) : base(dimension)
        {
            LoadedChunks = new LongHashSet<ChunkPosition>();
            ChunksToUnload = new LongHashSet<ChunkPosition>();
            ChunkLoadLists = new List<List<ChunkPosition>>();

            ChunkLoadSet = new LongHashSet<ChunkPosition>();
        }
    }
}
