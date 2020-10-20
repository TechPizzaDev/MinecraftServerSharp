using System;
using MCServerSharp.Components;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Maths;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    public class NetConnectionComponent : Component<NetConnection>, ITickable
    {
        private bool _firstSend = true;

        public GameTimeComponent GameTime { get; }

        /// <summary>
        /// The connection of this component.
        /// </summary>
        public NetConnection Connection => Entity;
        public ProtocolState ProtocolState => Connection.ProtocolState;

        public long TickCount { get; private set; }
        public float TickAliveTimer { get; private set; }

        public NetConnectionComponent(NetConnection connection) : base(connection)
        {
            GameTime = this.GetComponent<GameTimeComponent>();
        }

        public void Tick()
        {
            SendChunks();

            UpdateTickAlive();
        }

        private void UpdateTickAlive()
        {
            TickAliveTimer += GameTime.Delta;
            if (TickAliveTimer >= 1f)
            {
                if (ProtocolState == ProtocolState.Play)
                {
                    EnqueuePacket(new ServerKeepAlive(TickCount++));

                    var chat = Chat.Text(
                        $"S:{Connection.BytesSent / 1000}k | R:{Connection.BytesReceived / 100 / 10d}k");
                    EnqueuePacket(new ServerChat(chat, 2, UUID.Zero));
                }
                TickAliveTimer = 0;
            }
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Connection.EnqueuePacket(packet);
        }

        public void SendChunks()
        {
            if (!this.GetPlayer(out Player? player))
                return;

            if (_firstSend)
            {
                _firstSend = false;
                player.UpdateChunksToSend(8);
            }

            try
            {
                player.ChunkPosition = new ChunkPosition(
                    (int)(player.Position.X / 16),
                    (int)(player.Position.Z / 16));

                if (player.ChunkPosition != player.LastChunkPosition)
                {
                    Connection.EnqueuePacket(
                        new ServerUpdateViewPosition(
                            player.ChunkPosition.X,
                            player.ChunkPosition.Z));

                    Console.WriteLine(
                        player.UserName + " moved from chunk " +
                        player.LastChunkPosition + " to " + player.ChunkPosition);

                    if (Connection.GetComponent<ClientSettingsComponent>(out var clientSettings))
                    {
                        int viewDistance = clientSettings.Settings.ViewDistance;
                        player.UpdateChunksToSend(viewDistance);
                    }

                    player.LastChunkPosition = player.ChunkPosition;
                }

                // TODO: smarter sending
                // think about predicting player direction and sending chunks in front of player?

                int maxToSend = 6;
                var chunksToSend = player.ChunksToSend;
                for (int i = 0; i < chunksToSend.Count; i++)
                {
                    var cpos = chunksToSend[i];
                    chunksToSend.RemoveAt(i);

                    if (player.SentChunks.Add(cpos))
                    {
                        var chunk = player.Dimension.GetChunk(cpos);
                        var chunkData = new ServerChunkData(chunk, fullChunk: true);
                        Connection.EnqueuePacket(chunkData);

                        if (--maxToSend == 0)
                            goto End;
                    }
                }
                End:
                return;
            }
            catch (Exception ex)
            {
                Connection.Kick(ex);

                Console.WriteLine("Failed to send chunks to client: " + ex.Message);
            }
        }
    }
}
