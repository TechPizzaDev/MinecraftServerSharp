using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using MCServerSharp.Collections;
using MCServerSharp.Components;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Maths;
using MCServerSharp.Net.Packets;
using MCServerSharp.World;

namespace MCServerSharp.Net
{
    // TODO: respect view distance

    public class AsyncAutoResetEvent
    {
        private readonly Stack<ValueTaskSource> _pool = new();
        private readonly Queue<ValueTaskSource> _waits = new();
        private readonly bool _runContinuationsAsynchronously;
        private bool _signaled;

        public AsyncAutoResetEvent(bool runContinuationsAsynchronously = true)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
        }

        public ValueTask WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return new ValueTask(Task.CompletedTask);
                }
                else
                {
                    ValueTaskSource? tcs;
                    lock (_pool)
                    {
                        if (!_pool.TryPop(out tcs))
                            tcs = new ValueTaskSource();
                    }
                    tcs.RunContinuationsAsynchronously = _runContinuationsAsynchronously;

                    _waits.Enqueue(tcs);
                    return tcs.RunAsync();
                }
            }
        }

        public void Set()
        {
            ValueTaskSource? toRelease = null;
            lock (_waits)
            {
                if (!_waits.TryDequeue(out toRelease))
                {
                    if (!_signaled)
                        _signaled = true;
                }
            }

            if (toRelease != null)
            {
                toRelease.SetResult();

                if (_pool.Count < 4)
                {
                    lock (_pool)
                    {
                        _pool.Push(toRelease);
                    }
                }
            }
        }

        private sealed class ValueTaskSource : IValueTaskSource
        {
            private ManualResetValueTaskSourceCore<byte> _mrvtsc = new();

            public bool RunContinuationsAsynchronously
            {
                get => _mrvtsc.RunContinuationsAsynchronously;
                set => _mrvtsc.RunContinuationsAsynchronously = value;
            }

            public ValueTask RunAsync()
            {
                return new ValueTask(this, _mrvtsc.Version);
            }

            public void SetResult()
            {
                _mrvtsc.SetResult(0);
            }

            public void GetResult(short token)
            {
                _ = _mrvtsc.GetResult(token);
                _mrvtsc.Reset();
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return _mrvtsc.GetStatus(token);
            }

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                _mrvtsc.OnCompleted(continuation, state, token, flags);
            }
        }
    }

    public class NetConnectionComponent : Component<NetConnection>, ITickable
    {
        public int SendRate = 12;

        private bool _firstSend = true;
        private Task _sendTask;
        private AsyncAutoResetEvent _sendEvent = new();
        private List<ValueTask<IChunk>> _taskBuffer = new();

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

            _sendTask = SendTask();
        }

        public void Tick()
        {
            if (this.GetPlayer(out Player? player))
                UpdatePlayer(player);

            UpdateTickAlive();
        }

        private void UpdatePlayer(Player player)
        {
            if (Connection.GetComponent<ClientSettingsComponent>(out var clientSettings))
            {
                clientSettings.Tick();
            }

            if (ProtocolState == ProtocolState.Play)
            {
                _sendEvent.Set();
            }
        }

        private async Task SendTask()
        {
            while (true)
            {
                await _sendEvent.WaitAsync();

                if (this.GetPlayer(out Player? player))
                {
                    GatherChunksToSend(player);
                    await SendChunks(SendRate, player, _taskBuffer).Unchain();
                }
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
                $"{avgElapsed,2:0.0}ms | S:{UnitConvert.ToReadable(Connection.BytesSent)} | R:{UnitConvert.ToReadable(Connection.BytesReceived)}");
            EnqueuePacket(new ServerChat(chat, 2, UUID.Zero));
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Connection.EnqueuePacket(packet);
        }

        private async Task SendChunks(int maxChunksToSend, Player player, List<ValueTask<IChunk>> taskBuffer)
        {
            try
            {
                // TODO: smarter sending
                // think about predicting player direction and sending chunks in front of player?
                // also consider player look to prioritize chunks in front

                // TODO: get rid of player.ChunkLoadSet

                var chunksToUnload = player.ChunksToUnload;

                foreach (ChunkColumnPosition chunkPos in chunksToUnload)
                {
                    player.ChunkLoadSet.Remove(chunkPos);

                    if (player.LoadedChunks.Remove(chunkPos))
                    {
                        EnqueuePacket(new ServerUnloadChunk(chunkPos.X, chunkPos.Z));
                    }
                }
                chunksToUnload.Clear();

                // TODO: add a completion event to packets
                //  and add a completion to the last chunk packet
                //  to predict the connection speed and variate sending rate
                int maxToSend = maxChunksToSend;

                foreach (List<ChunkColumnPosition> loadList in player.ChunkLoadLists)
                {
                    for (int i = 0; i < loadList.Count; i++)
                    {
                        ChunkColumnPosition chunkPos = loadList[i];
                        loadList.RemoveAt(i);

                        if (!player.ChunkLoadSet.Remove(chunkPos))
                            continue;

                        if (player.LoadedChunks.Add(chunkPos))
                        {
                            IChunkColumn column = await player.Dimension.GetOrAddChunkColumn(chunkPos).Unchain();

                            var localColumn = (LocalChunkColumn)column;
                            await SendChunkColumn(localColumn, taskBuffer).Unchain();

                            player.SentColumnCount++;

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

                Console.WriteLine("Failed to send chunks to client: " + ex);
            }
        }

        public static int GetChebyshevDistance(ChunkColumnPosition a, ChunkColumnPosition b)
        {
            int dX = a.X - b.X;
            int dZ = a.Z - b.Z;
            return Math.Max(Math.Abs(dX), Math.Abs(dZ));
        }

        private static void SendAllChunks(Player player, ChunkColumnPosition origin, int viewDistance)
        {
            for (int x = origin.X - viewDistance; x <= origin.X + viewDistance; x++)
            {
                for (int z = origin.Z - viewDistance; z <= origin.Z + viewDistance; z++)
                {
                    var position = new ChunkColumnPosition(x, z);
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

            ChunkColumnPosition previousPos = player.CameraPosition;
            ChunkColumnPosition currentPos = player.ChunkPosition;

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

                    //Console.WriteLine(
                    //    player.UserName + " moved from chunk " +
                    //    lastCameraPos + " to " + player.CameraPosition);
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
                        ChunkColumnPosition chunkPos = new(x, z);
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
                        player.ChunksToUnload.Add(new ChunkColumnPosition(x, z));
                    }
                }

                SendAllChunks(player, currentPos, viewDistance);
            }



            List<List<ChunkColumnPosition>> loadLists = player.ChunkLoadLists;
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

                List<ChunkColumnPosition> loadList = loadLists[i];
                for (int j = 0; j < posCount; j++)
                {
                    ChunkColumnPosition chunkPos = loadList[j];
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

        private static void EnqueueChunkForLoad(Player player, ChunkColumnPosition position)
        {
            int distance = GetChebyshevDistance(player.CameraPosition, position);

            while (player.ChunkLoadLists.Count <= distance)
                player.ChunkLoadLists.Add(new List<ChunkColumnPosition>());

            List<ChunkColumnPosition> loadList = player.ChunkLoadLists[distance];
            loadList.Add(position);
        }

        private async ValueTask SendChunkColumn(LocalChunkColumn chunkColumn, List<ValueTask<IChunk>> taskBuffer)
        {
            ValueTask<IChunk>[] chunks = new ValueTask<IChunk>[chunkColumn.GetMaxChunkCount()];
            chunkColumn.GetOrAddChunks(chunks);

            var skyLights = new List<LightArray>();
            var blockLights = new List<LightArray>();

            BitSet skyLightMask = new(chunks.Length);
            BitSet blockLightMask = new(chunks.Length);
            BitSet emptySkyLightMask = new(chunks.Length);
            BitSet emptyBlockLightMask = new(chunks.Length);

            ArrayPool<byte> pool = ArrayPool<byte>.Shared;

            for (int i = 0; i < chunks.Length; i++)
            {
                LocalChunk chunk = (LocalChunk)await chunks[i].Unchain();

                if (chunk.SkyLight == null)
                {
                    emptySkyLightMask[i] = true;
                }
                else
                {
                    skyLightMask[i] = true;
                    var skyLight = new LightArray(pool);
                    chunk.SkyLight.CopyTo(skyLight.Data);
                    skyLights.Add(skyLight);
                }

                if (chunk.BlockLight == null)
                {
                    emptyBlockLightMask[i] = true;
                }
                else
                {
                    blockLightMask[i] = true;
                    var blockLight = new LightArray(pool);
                    chunk.BlockLight.CopyTo(blockLight.Data);
                    blockLights.Add(blockLight);
                }
            }

            var light = new LightUpdate(
                true,
                skyLightMask,
                blockLightMask,
                emptySkyLightMask,
                emptyBlockLightMask,
                skyLights, blockLights, pool);
            //Connection.EnqueuePacket(light);

            var chunkData = new ServerChunkData(chunkColumn, light);
            Connection.EnqueuePacket(chunkData);
        }
    }
}
