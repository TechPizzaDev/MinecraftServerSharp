﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using MCServerSharp.Data;
using MCServerSharp.Net;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;
using MCServerSharp.World;

namespace MCServerSharp.Runner
{
    internal static class Runner
    {
        // TODO: move these to a Game class
        public const string PongResource = "Minecraft/Net/Pong.json";

        private static long _tickCount;
        private static NetManager _manager;
        private static string? _requestPongBase;

        private static Dimension _dimension = new Dimension();

        private static List<NetConnection> _connectionsBuffer = new List<NetConnection>();

        private static void Main(string[] args)
        {
            #region NBT Testing
            // TODO: move to sandbox

            //var motionBlocking = new NbtLongArray(36, "MOTION_BLOCKING");
            //var mem = new MemoryStream();
            //var writer = new NetBinaryWriter(mem);
            //writer.Write(motionBlocking.AsCompound("Heightmaps"));
            //var document = NbtDocument.Parse(mem.GetBuffer().AsMemory(0, (int)mem.Length));

            //NbtDocument document = null;
            //
            //if (false)
            //{
            //    document = NbtDocument.Parse(File.ReadAllBytes(@"C:\Users\Michal Piatkowski\Downloads\hello_world.nbt"));
            //}
            //else
            //{
            //    using (var fs = File.OpenRead(@"C:\Users\Michal Piatkowski\Downloads\bigtest.nbt"))
            //    using (var ds = new GZipStream(fs, CompressionMode.Decompress))
            //    using (var ms = new MemoryStream())
            //    {
            //        ds.SCopyTo(ms);
            //        var memory = ms.GetBuffer().AsMemory(0, (int)ms.Length);
            //
            //        //var reader = new NbtReader(memory.Span);
            //        //while (reader.Read())
            //        //{
            //        //    //Console.WriteLine(reader.NameSpan.ToUtf8String() + ": " + reader.TagType);
            //        //}
            //
            //        //for (int i = 0; i < 1_000_000; i++)
            //        {
            //            document = NbtDocument.Parse(memory);
            //            //document.Dispose();
            //            //Thread.Sleep(i % 100 == 0 ? 1 : 0);
            //        }
            //    }
            //
            //    //return;
            //
            //    Console.WriteLine();
            //    Console.WriteLine(new string('-', 20));
            //    Console.WriteLine();
            //}
            //
            //var root = document.RootTag;
            //
            //Console.WriteLine(root);
            //
            //void Log(NbtElement element, int depth = 0)
            //{
            //    string depthPad = new string(' ', depth * 3);
            //
            //    foreach (var item in element.EnumerateContainer())
            //    {
            //        Console.WriteLine(depthPad + item);
            //
            //        if (item.Type.IsContainer())
            //        {
            //            for (int i = 0; i < item.GetLength(); i++)
            //            {
            //                Console.WriteLine(depthPad + "INDEXER: " + item[i]);
            //            }
            //
            //            Log(item, depth + 1);
            //        }
            //    }
            //}
            //Log(root);
            #endregion

            var configProvider = new FallbackResourceProvider(
                new FileResourceProvider("Config", includeDirectoryName: false),
                new AssemblyResourceProvider(
                    Assembly.GetExecutingAssembly(), "MCServerSharp.Runner.Templates.Config"));

            using var pong = configProvider.OpenResourceReader(PongResource);
            if (pong == null)
                throw new KeyNotFoundException(PongResource);
            _requestPongBase = pong.ReadToEnd();

            var ticker = new Ticker(targetTickTime: TimeSpan.FromMilliseconds(50));

            _manager = new NetManager();
            _manager.Listener.Connection += Manager_Connection;
            _manager.Listener.Disconnection += Manager_Disconnection;
            SetPacketHandlers(_manager);

            ushort port = 25566;
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            _manager.Bind(localEndPoint);
            Console.WriteLine("Listener bound to endpoint " + localEndPoint);

            Console.WriteLine("Setting up network manager...");
            _manager.Setup();

            int backlog = 200;
            Console.WriteLine("Listener backlog queue size: " + backlog);

            _manager.Listen(backlog);
            Console.WriteLine("Listening for connections...");

            ticker.Tick += Game_Tick;

            ticker.Run();

            Console.ReadKey();
            return;
        }

        private static void Game_Tick(Ticker ticker)
        {
            _tickCount++;
            if (_tickCount % 20 == 0) // Every second
            {
                _connectionsBuffer.Clear();
                _manager.UpdateConnections(_connectionsBuffer, out int activeCount);

                foreach (var conn in _connectionsBuffer)
                    TickAlive(conn, _tickCount);

                //if (updateCount > 0)
                //    Console.WriteLine(activeCount + " connections");

                lock (_manager.ConnectionMutex)
                {
                    foreach (var connection in _manager.Connections)
                    {
                        if (connection.ProtocolState != ProtocolState.Play)
                            continue;

                        var chat = Chat.Text($"S:{connection.BytesSent / 1000}k | R:{connection.BytesReceived / 100 / 10d}k");

                        connection.EnqueuePacket(new ServerChat(chat, 2));
                    }
                }

                Console.WriteLine(
                    "Tick Time: " +
                    ticker.ElapsedTime.TotalMilliseconds.ToString("00.0") +
                    "/" +
                    ticker.TargetTime.TotalMilliseconds.ToString("00") + " ms" +
                    " | " +
                    (ticker.ElapsedTime.Ticks / (float)ticker.TargetTime.Ticks * 100f).ToString("00") + "%");
            }

            _dimension.Tick();
        }

        public static void TickAlive(NetConnection connection, long keepAliveId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.ProtocolState == ProtocolState.Play)
                connection.EnqueuePacket(new ServerKeepAlive(keepAliveId));
        }

        private static void SetPacketHandlers(NetManager manager)
        {
            manager.Codec.LegacyServerListPingHandler = delegate
                (NetConnection connection, ClientLegacyServerListPing? ping)
            {
                bool isBeta = !ping.HasValue;

                string motd = "A minecraft server";
                if (isBeta && manager.Config_AppendGameVersionToBetaStatus)
                    motd = motd + " - " + manager.GameVersion;

                var answer = new ServerLegacyServerListPong(
                    isBeta, manager.ProtocolVersion, manager.GameVersion, motd, 0, 100);

                connection.Close(immediate: false);
            };

            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientRequest request)
            {
                if (_requestPongBase == null)
                    return;

                // TODO: make these dynamic
                var strComparison = StringComparison.OrdinalIgnoreCase;
                var numFormat = NumberFormatInfo.InvariantInfo;

                // TODO: better config
                string jsonResponse = _requestPongBase
                    .Replace("%version%", manager.GameVersion.ToString(), strComparison)
                    .Replace("\"%versionID%\"", manager.ProtocolVersion.ToString(numFormat), strComparison)
                    .Replace("\"%max%\"", 20.ToString(numFormat), strComparison)
                    .Replace("\"%online%\"", 0.ToString(numFormat), strComparison);

                var answer = new ServerResponse((Utf8String)jsonResponse);
                connection.EnqueuePacket(answer);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientHandshake handshake)
            {
                if (handshake.NextState != ProtocolState.Status &&
                    handshake.NextState != ProtocolState.Login)
                    return;

                connection.ProtocolState = handshake.NextState;
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPing ping)
            {
                var answer = new ServerPong(ping.Payload);
                connection.EnqueuePacket(answer);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientLoginStart loginStart)
            {
                var setCompression = new ServerSetCompression(128);
                connection.EnqueuePacket(setCompression);
                connection.CompressionThreshold = setCompression.Threshold;

                var uuid = new UUID(0, 1);
                var name = loginStart.Name;
                var loginSuccess = new ServerLoginSuccess(uuid.ToUtf8String(), name);

                connection.EnqueuePacket(loginSuccess);

                connection.UserName = name.ToString();
                connection.ProtocolState = ProtocolState.Play;

                var playerId = new EntityId(69);

                connection.EnqueuePacket(new ServerJoinGame(
                    playerId.Value, 1, 0, 0, 0, (Utf8String)"default", 16, false, true));

                connection.EnqueuePacket(new ServerPluginMessage(
                    (Utf8String)"minecraft:brand",
                    (Utf8String)"MinecraftServerSharp"));

                connection.EnqueuePacket(new ServerSpawnPosition(
                    new Position(64, 260, 64)));

                connection.EnqueuePacket(new ServerPlayerPositionLook(
                    64, 260, 64, 0, 0, ServerPlayerPositionLook.PositionRelatives.None, 1337));

                connection.EnqueuePacket(new ServerPlayerAbilities(
                    ServerAbilityFlags.AllowFlying | ServerAbilityFlags.Flying,
                    0.2f,
                    0.1f));

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int z = 0; z < 8; z++)
                        {
                            for (int x = 0; x < 8; x++)
                            {
                                if (connection.ProtocolState != ProtocolState.Play)
                                    goto End;

                                var chunk = _dimension.GetChunk(x, z);
                                var chunkData = new ServerChunkData(chunk, fullChunk: true);
                                connection.EnqueuePacket(chunkData);
                            }
                        }

                        End:
                        return;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
            });


            static void PlayerPositionChange(NetConnection connection, double x, double y, double z)
            {
                connection.EnqueuePacket(
                    new ServerUpdateViewPosition((VarInt)(x / 16), (VarInt)(z / 16)));
            }


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientUseItem useItem)
            {
                Console.WriteLine("item used");
            });


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientPlayerDigging playerAbilities)
            {
                Console.WriteLine("digging status: " + playerAbilities.Status);
            });


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientPlayerAbilities playerAbilities)
            {

            });


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientCreativeInventoryAction creativeInventoryAction)
            {
                Console.WriteLine(
                    creativeInventoryAction.Slot + ": " + creativeInventoryAction.SlotData.Present);
            });


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientClickWindow clickWindow)
            {
                Console.WriteLine(clickWindow.Slot);
            });


            manager.SetPacketHandler(delegate (
                NetConnection connection, ClientPlayerBlockPlacement playerBlockPlacement)
            {
                byte windowID = 1;

                //Console.WriteLine(playerBlockPlacement.);
                connection.EnqueuePacket(new ServerOpenWindow(windowID, 13, Chat.Text("Inv on place")));

                System.Threading.Tasks.Task.Run(() =>
                {
                    connection.EnqueuePacket(new ServerWindowProperty(windowID, 3, 100));

                    float x = 0;
                    while (x < 20)
                    {
                        short value = (short)((Math.Sin(x) + 1) * 50);
                        connection.EnqueuePacket(new ServerWindowProperty(windowID, 2, value));

                        x += 0.2f;
                        Thread.Sleep(50);
                    }
                });
            });

            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientTeleportConfirm teleportConfirm)
            {
                //Console.WriteLine("Teleport Confirm: Id " + teleportConfirm.TeleportId);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPlayerPosition playerPosition)
            {
                //Console.WriteLine(
                //    "Player Position:" +
                //    " X" + playerPosition.X +
                //    " Y" + playerPosition.FeetY +
                //    " Z" + playerPosition.Z);

                PlayerPositionChange(connection, playerPosition.X, playerPosition.FeetY, playerPosition.Z);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPlayerMovement playerMovement)
            {
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPlayerPositionRotation playerPositionRotation)
            {
                //Console.WriteLine(
                //    "Player Position Rotation:" // +
                //                                //" X" + playerPosition.X +
                //                                //" Y" + playerPosition.FeetY +
                //                                //" Z" + playerPosition.Z
                //    );

                PlayerPositionChange(
                    connection, playerPositionRotation.X, playerPositionRotation.FeetY, playerPositionRotation.Z);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPlayerRotation playerRotation)
            {
                //Console.WriteLine(
                //    "Player Rotation:" +
                //    " Yaw" + playerRotation.Yaw +
                //    " Pitch" + playerRotation.Pitch);
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientClientSettings clientSettings)
            {
                Console.WriteLine("Got client settings");
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientCloseWindow closeWindow)
            {

            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientEntityAction entityAction)
            {
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientAnimation animation)
            {

            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientHeldItemChange heldItemChange)
            {

            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientRecipeBookData recipeBookData)
            {
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientChat chat)
            {
                // TODO: better broadcasting

                string? name = connection.UserName;
                if (name == null)
                    name = "null";

                Console.WriteLine("<" + name + ">: " + chat.Message);

                var dyn = new
                {
                    translate = "chat.type.text",
                    with = new[]
                    {
                        new { text = name, color = "red" },
                        new { text = chat.Message.ToString(), color = "reset" }
                    }
                };

                var chatToSend = new Chat(new Utf8String(JsonSerializer.Serialize(dyn)));

                lock (manager.ConnectionMutex)
                {
                    foreach (var conn in manager.Connections)
                    {
                        conn.EnqueuePacket(new ServerChat(chatToSend, 0));
                    }
                }
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPluginMessage pluginMessage)
            {

            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientKeepAlive pluginMessage)
            {

            });
        }

        private static void Manager_Connection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Connection: " + connection.RemoteEndPoint);
        }

        private static void Manager_Disconnection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Disconnection: " + connection.RemoteEndPoint);
        }
    }
}
