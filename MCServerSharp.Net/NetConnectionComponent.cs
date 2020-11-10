using System;
using System.Collections.Generic;
using MCServerSharp.Components;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Maths;
using MCServerSharp.Net.Packets;
using MCServerSharp.World;

namespace MCServerSharp.Net
{
    // TODO: respect view distance

    public class NetConnectionComponent : Component<NetConnection>, ITickable
    {
        private bool _firstSend = true;

        public GameTimeComponent GameTime { get; }

        /// <summary>
        /// The connection of this component.
        /// </summary>
        public NetConnection Connection => Entity;
        public ProtocolState ProtocolState => Connection.ProtocolState;

        public long AliveTickCount { get; private set; }
        public float TickAliveTimer { get; private set; }

        public NetConnectionComponent(NetConnection connection) : base(connection)
        {
            GameTime = this.GetComponent<GameTimeComponent>();
        }

        public void Tick()
        {
            if (this.GetPlayer(out Player? player))
                UpdatePlayer(player);

            UpdateTickAlive();
        }

        public void UpdatePlayer(Player player)
        {
            if (Connection.GetComponent<ClientSettingsComponent>(out var clientSettings))
            {
                clientSettings.Tick();
            }

            if (ProtocolState == ProtocolState.Play)
            {
                GatherChunksToSend(player);
                SendChunks(player);
            }
        }

        private void UpdateTickAlive()
        {
            TickAliveTimer += GameTime.Delta;
            if (TickAliveTimer >= 1f)
            {
                if (ProtocolState == ProtocolState.Play)
                {
                    EnqueuePacket(new ServerKeepAlive(AliveTickCount++));
                }
                TickAliveTimer = 0;
            }

            double avgElapsed = GameTime.Ticker.AverageElapsedTime.TotalMilliseconds;

            var chat = Chat.Text(
                $"{avgElapsed,2:0}ms | S:{ToReadable(Connection.BytesSent)} | R:{ToReadable(Connection.BytesReceived)}");
            EnqueuePacket(new ServerChat(chat, 2, UUID.Zero));
        }

        static string[] sizeSuffixes = { "", "K", "M", "G", "T", "P" };

        private static string ToReadable(long byteCount)
        {
            int order = 0;
            double length = byteCount;
            while (length >= 1000 && order < sizeSuffixes.Length - 1)
            {
                order++;
                length /= 1000;
            }

            // "{0:0.#}{1}" would show a single decimal place, and no space.
            int decimalCount = Math.Max(0, (int)Math.Ceiling(2 - Math.Log10(length))); // length < 10 ? 2 : 1;
            string decimals = new string('0', decimalCount);
            string result = string.Format($"{{0:0.{decimals}}}{{1}}", length, sizeSuffixes[order]);
            return result;
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Connection.EnqueuePacket(packet);
        }

        public void SendChunks(Player player)
        {
            try
            {
                // TODO: smarter sending
                // think about predicting player direction and sending chunks in front of player?
                // also consider player look to prioritize chunks in front

                // TODO: get rid of player.ChunkLoadSet

                var chunksToUnload = player.ChunksToUnload;

                foreach (var chunkPos in chunksToUnload)
                {
                    player.ChunkLoadSet.Remove(chunkPos);

                    if (player.LoadedChunks.Remove(chunkPos))
                    {
                        EnqueuePacket(new ServerUnloadChunk(chunkPos.X, chunkPos.Z));
                    }
                }
                chunksToUnload.Clear();

                int maxToSend = 5;

                foreach (var loadList in player.ChunkLoadLists)
                {
                    for (int i = 0; i < loadList.Count; i++)
                    {
                        var chunkPos = loadList[i];
                        loadList.RemoveAt(i);

                        if (!player.ChunkLoadSet.Remove(chunkPos))
                            continue;

                        if (player.LoadedChunks.Add(chunkPos))
                        {
                            var chunk = player.Dimension.GetChunk(chunkPos);
                            SendChunk(chunk);
                            player.SentChunkCount++;

                            if (--maxToSend == 0)
                                goto End;
                        }
                    }
                    continue;

                    End:
                    for (int i = loadList.Count; i-- > 0;)
                    {
                        if (!player.ChunkLoadSet.Contains(loadList[i]))
                            loadList.RemoveAt(i);
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Connection.Kick(ex);

                Console.WriteLine("Failed to send chunks to client: " + ex.Message);
            }
        }

        public static int GetChebyshevDistance(ChunkPosition a, ChunkPosition b)
        {
            int dX = a.X - b.X;
            int dZ = a.Z - b.Z;
            return Math.Max(Math.Abs(dX), Math.Abs(dZ));
        }

        private void SendAllChunks(Player player, ChunkPosition origin, int viewDistance)
        {
            for (int x = origin.X - viewDistance; x <= origin.X + viewDistance; x++)
            {
                for (int z = origin.Z - viewDistance; z <= origin.Z + viewDistance; z++)
                {
                    var position = new ChunkPosition(x, z);
                    if (player.ChunkLoadSet.Add(position))
                    {
                        EnqueueChunkForLoad(player, position);
                    }
                }
            }
        }

        private void GatherChunksToSend(Player player)
        {
            int viewDistance = player.ViewDistance;

            ChunkPosition previousPos = player.CameraPosition;
            ChunkPosition currentPos = player.ChunkPosition;

            if (_firstSend)
            {
                player.ScheduleFullChunkView = true;
                _firstSend = false;
            }

            bool positionChanged = previousPos != currentPos;
            bool shouldSend = player.ScheduleFullChunkView || positionChanged;

            if (player.ScheduleFullChunkView)
            {
                SendAllChunks(player, currentPos, viewDistance);
                player.ScheduleFullChunkView = false;
            }

            if (positionChanged)
            {
                {
                    var lastCameraPos = player.CameraPosition;
                    player.CameraPosition = currentPos;

                    Connection.EnqueuePacket(
                        new ServerUpdateViewPosition(
                            player.CameraPosition.X,
                            player.CameraPosition.Z));

                    Console.WriteLine(
                        player.UserName + " moved from chunk " +
                        lastCameraPos + " to " + player.CameraPosition);
                }
            }

            if (!shouldSend)
                return;

            if (Math.Abs(previousPos.X - currentPos.X) <= viewDistance * 2 &&
                Math.Abs(previousPos.Z - currentPos.Z) <= viewDistance * 2)
            {
                int minX = Math.Min(currentPos.X, previousPos.X) - viewDistance;
                int minZ = Math.Min(currentPos.Z, previousPos.Z) - viewDistance;
                int maxX = Math.Max(currentPos.X, previousPos.X) + viewDistance;
                int maxZ = Math.Max(currentPos.Z, previousPos.Z) + viewDistance;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var chunkPos = new ChunkPosition(x, z);
                        bool withinView = GetChebyshevDistance(chunkPos, currentPos) <= viewDistance;
                        bool withinMaxView = GetChebyshevDistance(chunkPos, previousPos) <= viewDistance;

                        if (withinView && !withinMaxView)
                        {
                            if (player.ChunkLoadSet.Add(chunkPos))
                                EnqueueChunkForLoad(player, chunkPos);
                        }

                        if (!withinView && withinMaxView)
                        {
                            player.ChunksToUnload.Add(chunkPos);
                        }
                    }
                }
            }
            else
            {
                for (int x = previousPos.X - viewDistance; x <= previousPos.X + viewDistance; x++)
                {
                    for (int z = previousPos.Z - viewDistance; z <= previousPos.Z + viewDistance; z++)
                    {
                        player.ChunksToUnload.Add(new ChunkPosition(x, z));
                    }
                }

                SendAllChunks(player, currentPos, viewDistance);
            }



            var loadLists = player.ChunkLoadLists;
            Span<int> posCounts = stackalloc int[loadLists.Count];

            // Gather all position counts for later.
            for (int i = 0; i < posCounts.Length; i++)
                posCounts[i] = loadLists[i].Count;

            for (int i = 0; i < posCounts.Length; i++)
            {
                int posCount = posCounts[i];
                if (posCount == 0)
                    continue;

                // Iterate positions of the current list and enqueue them to update distances.
                // We use a "fixed" count in the loop as a position may be readded to the current list.
                // If distance didn't change, the position is appended after the current positions.

                var loadList = loadLists[i];
                for (int j = 0; j < posCount; j++)
                {
                    var chunkPos = loadList[j];
                    if (player.ChunksToUnload.Remove(chunkPos))
                    {
                        // We can skip sending the chunk as it was queued for unload.
                        player.ChunkLoadSet.Remove(chunkPos);
                    }
                    else
                    {
                        EnqueueChunkForLoad(player, chunkPos);
                    }
                }

                // Remove all old positions as they are now in front of the new positions.
                loadList.RemoveRange(0, posCount);
            }
        }

        private static void EnqueueChunkForLoad(Player player, ChunkPosition position)
        {
            int distance = GetChebyshevDistance(player.CameraPosition, position);

            while (player.ChunkLoadLists.Count <= distance)
                player.ChunkLoadLists.Add(new List<ChunkPosition>());

            var loadList = player.ChunkLoadLists[distance];
            loadList.Add(position);
        }

        private void SendChunk(Chunk chunk)
        {
            var sections = chunk.Sections.Span;

            var skyLights = new List<LightArray>();
            var blockLights = new List<LightArray>();

            var skyLightMask = 0;
            var filledSkyLightMask = 0;
            var blockLightMask = 0;
            var filledBlockLightMask = 0;
            for (int s = 0; s < 3; s++)
            {
                bool filled = true;
                if (s > 0 && s < 16)
                {
                    filled = !sections[s - 1].IsEmpty;

                    skyLightMask |= 1 << s;
                    skyLights.Add(new LightArray());

                    //blockLightMask |= 1 << s;
                    //blockLights.Add(new LightArray());
                    filledBlockLightMask |= 1 << s;
                }
                else
                {
                    filledSkyLightMask |= 1 << s;
                }

                if (filled)
                    filledBlockLightMask |= 1 << s;
            }

            var updateLight = new ServerUpdateLight(
                chunk.X, chunk.Z, true,
                skyLightMask, blockLightMask,
                filledSkyLightMask, filledBlockLightMask,
                skyLights, blockLights);
            Connection.EnqueuePacket(updateLight);

            var chunkData = new ServerChunkData(chunk, 65535);
            Connection.EnqueuePacket(chunkData);
        }
    }
}
