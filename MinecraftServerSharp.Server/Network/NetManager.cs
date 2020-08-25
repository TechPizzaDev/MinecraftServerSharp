using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using MinecraftServerSharp.Collections;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;
using MinecraftServerSharp.World;

namespace MinecraftServerSharp.Net
{
    public class NetManager
    {
        public const string PongResource = "Minecraft/Net/Pong.json";

        // TODO: move these somewhere
        public int ProtocolVersion { get; } = 578;
        public MinecraftVersion GameVersion { get; } = new MinecraftVersion(1, 15, 2);
        public bool Config_AppendGameVersionToBetaStatus { get; } = true;

        private HashSet<NetConnection> _connections;
        private string? _requestPongBase;

        public NetProcessor Processor { get; }
        public NetOrchestrator Orchestrator { get; }
        public NetListener Listener { get; }

        public object ConnectionMutex { get; } = new object();
        public ReadOnlySet<NetConnection> Connections { get; }

        public NetManager()
        {
            Processor = new NetProcessor();
            Orchestrator = new NetOrchestrator(Processor);
            Listener = new NetListener(Orchestrator);

            _connections = new HashSet<NetConnection>();
            Connections = _connections.AsReadOnly();
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            Listener.Bind(localEndPoint);
        }

        public void Setup()
        {
            Processor.SetupCodecs();

            SetupPacketHandlers();
        }

        public void SetConfig(IResourceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            using var pong = provider.OpenResourceReader(PongResource);
            if (pong == null)
                throw new KeyNotFoundException(PongResource);

            _requestPongBase = pong.ReadToEnd();
        }

        private void SetPacketHandler<TPacket>(ClientPacketId id, Action<NetConnection, TPacket> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Processor.SetPacketHandler(id, (connection, rawId, definition) =>
            {
                var (status, length) = connection.ReadPacket<TPacket>(out var packet);
                if (status == OperationStatus.Done)
                {
                    handler.Invoke(connection, packet);
                }
            });
        }

        private void SetPacketHandler<TPacket>(Action<NetConnection, TPacket> handler)
        {
            var packetStruct = typeof(TPacket).GetCustomAttribute<PacketStructAttribute>();
            if (packetStruct == null)
                throw new ArgumentException($"The type is missing a \"{nameof(PacketStructAttribute)}\".");

            if (!packetStruct.IsClientPacket)
                throw new ArgumentException("The packet is not a client packet.");

            SetPacketHandler((ClientPacketId)packetStruct.PacketId, handler);
        }

        public void Listen(int backlog)
        {
            // TODO: fix some kind of concurrency that corrupts sent data
            Orchestrator.Start(workerCount: 1);

            Listener.Connection += Listener_Connection;
            Listener.Disconnection += Listener_Disconnection;

            Listener.Start(backlog);
        }

        public void Flush()
        {
            Orchestrator.Flush();
        }

        private void Listener_Connection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Add(connection))
                    throw new InvalidOperationException();
            }

            Processor.AddConnection(connection);
        }

        private void Listener_Disconnection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Remove(connection))
                    throw new InvalidOperationException();
            }
        }

        public int GetConnectionCount()
        {
            lock (ConnectionMutex)
            {
                return _connections.Count;
            }
        }

        private void SetupPacketHandlers()
        {
            Processor.LegacyServerListPingHandler = delegate (NetConnection connection, ClientLegacyServerListPing? ping)
            {
                bool isBeta = !ping.HasValue;

                string motd = "A minecraft server";
                if (isBeta && Config_AppendGameVersionToBetaStatus)
                    motd = motd + " - " + GameVersion;

                var answer = new ServerLegacyServerListPong(
                    isBeta, ProtocolVersion, GameVersion, motd, 0, 100);

                connection.EnqueuePacket(answer);

                connection.State = ProtocolState.Closing;
            };


            SetPacketHandler(delegate (NetConnection connection, ClientRequest request)
            {
                if (_requestPongBase == null)
                    return;

                // TODO: make these dynamic
                var strComparison = StringComparison.OrdinalIgnoreCase;
                var numFormat = NumberFormatInfo.InvariantInfo;

                // TODO: better config
                string jsonResponse = _requestPongBase
                    .Replace("%version%", GameVersion.ToString(), strComparison)
                    .Replace("\"%versionID%\"", ProtocolVersion.ToString(numFormat), strComparison)
                    .Replace("\"%max%\"", 20.ToString(numFormat), strComparison)
                    .Replace("\"%online%\"", 0.ToString(numFormat), strComparison);

                var answer = new ServerResponse((Utf8String)jsonResponse);
                connection.EnqueuePacket(answer);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientHandshake handshake)
            {
                if (handshake.NextState != ProtocolState.Status &&
                    handshake.NextState != ProtocolState.Login)
                    return;

                connection.State = handshake.NextState;
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPing ping)
            {
                var answer = new ServerPong(ping.Payload);
                connection.EnqueuePacket(answer);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientLoginStart loginStart)
            {
                var uuid = new UUID(0, 1);

                var name = loginStart.Name;
                var answer = new ServerLoginSuccess(uuid.ToUtf8String(), name);
                connection.UserName = loginStart.Name.ToString();

                connection.EnqueuePacket(answer);

                connection.State = ProtocolState.Play;

                var playerId = new EntityId(69);

                connection.EnqueuePacket(new ServerJoinGame(
                    playerId.Value, 3, 0, 0, 0, (Utf8String)"default", 16, false, true));

                connection.EnqueuePacket(new ServerPluginMessage(
                    (Utf8String)"minecraft:brand",
                    (Utf8String)"MinecraftServerSharp"));

                connection.EnqueuePacket(new ServerSpawnPosition(
                    new Position(0, 128, 0)));

                connection.EnqueuePacket(new ServerPlayerPositionLook(
                    0, 128, 0, 0, 0, ServerPlayerPositionLook.PositionRelatives.None, 1337));

                var palette = new DirectBlockPalette();
                uint num = 100;
                for (uint j = 0; j < num; j++)
                {
                    //if (j == 6)
                    //    continue;

                    var state = new BlockState();
                    palette._stateToId.Add(state, j);
                    palette._idToState.Add(j, state);
                }

                uint i = 0;
                var dimension = new Dimension();
                for (int z = 0; z < 8; z++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        var chunk = new Chunk(x, z, dimension, palette);

                        foreach (var section in chunk.Sections.Span)
                        {
                            if (palette._idToState.ContainsKey(i))
                            {
                                section.Fill(palette._idToState[i]);

                                i = (i + 1) % num;
                            }
                        }

                        var chunkData = new ServerChunkData(chunk, fullChunk: true);
                        connection.EnqueuePacket(chunkData);
                    }
                }
            });


            void PlayerPositionChange(NetConnection connection, double x, double y, double z)
            {
                connection.EnqueuePacket(
                    new ServerUpdateViewPosition((VarInt)(x / 16), (VarInt)(z / 16)));
            }


            SetPacketHandler(delegate (NetConnection connection, ClientTeleportConfirm teleportConfirm)
            {
                Console.WriteLine("Teleport Confirm: Id " + teleportConfirm.TeleportId);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPlayerMovement playerMovement)
            {
                // This will always be false right now as the player is always flying
                Console.WriteLine("player is " + (playerMovement.OnGround ? " " : "not") + "on ground"); 
                // We safely ignore this packet right now as it isn't used for much
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPlayerPosition playerPosition)
            {
                Console.WriteLine(
                    "Player Position:" +
                    " X" + playerPosition.X +
                    " Y" + playerPosition.FeetY +
                    " Z" + playerPosition.Z);

                PlayerPositionChange(connection, playerPosition.X, playerPosition.FeetY, playerPosition.Z);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPlayerPositionRotation playerPositionRotation)
            {
                Console.WriteLine(
                    "Player Position Rotation:" // +
                                                //" X" + playerPosition.X +
                                                //" Y" + playerPosition.FeetY +
                                                //" Z" + playerPosition.Z
                    );

                PlayerPositionChange(
                    connection, playerPositionRotation.X, playerPositionRotation.FeetY, playerPositionRotation.Z);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPlayerRotation playerRotation)
            {
                Console.WriteLine(
                    "Player Rotation:" +
                    " Yaw" + playerRotation.Yaw +
                    " Pitch" + playerRotation.Pitch);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientClientSettings clientSettings)
            {
                Console.WriteLine("Got client settings");
            });


            SetPacketHandler(delegate (NetConnection connection, ClientCloseWindow closeWindow)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientEntityAction entityAction)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientAnimation animation)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientHeldItemChange heldItemChange)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientRecipeBookData recipeBookData)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientChat chat)
            {
                // TODO broadcast to everyone
                Console.WriteLine("<" + connection.UserName + ">: " + chat.Message);
            });


            SetPacketHandler(delegate (NetConnection connection, ClientPluginMessage pluginMessage)
            {

            });


            SetPacketHandler(delegate (NetConnection connection, ClientKeepAlive pluginMessage)
            {

            });
        }

        public void TickAlive(long keepAliveId)
        {
            lock (ConnectionMutex)
            {
                foreach (NetConnection connection in Connections)
                {
                    connection.EnqueuePacket(new ServerKeepAlive(keepAliveId));
                }
            }
        }
    }
}