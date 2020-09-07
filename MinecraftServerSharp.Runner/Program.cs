using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using MinecraftServerSharp.Data;
using MinecraftServerSharp.Net;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;
using MinecraftServerSharp.World;

namespace MinecraftServerSharp.Runner
{
    internal class Program
    {
        // TODO: move these to a Game class
        public const string PongResource = "Minecraft/Net/Pong.json";
        private static long tickCount;
        private static Random rng = new Random();
        private static NetManager manager;
        private static string _requestPongBase;

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
                    Assembly.GetExecutingAssembly(), "MinecraftServerSharp.Runner.Templates.Config"));

            using var pong = configProvider.OpenResourceReader(PongResource);
            if (pong == null)
                throw new KeyNotFoundException(PongResource);
            _requestPongBase = pong.ReadToEnd();

            var ticker = new Ticker(targetTickTime: TimeSpan.FromMilliseconds(50));

            manager = new NetManager();
            manager.Listener.Connection += Manager_Connection;
            manager.Listener.Disconnection += Manager_Disconnection;
            SetPacketHandlers(manager);

            ushort port = 25565;
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            manager.Bind(localEndPoint);
            Console.WriteLine("Listener bound to endpoint " + localEndPoint);

            Console.WriteLine("Setting up network manager...");
            manager.Setup();

            int backlog = 200;
            Console.WriteLine("Listener backlog queue size: " + backlog);

            manager.Listen(backlog);
            Console.WriteLine("Listening for connections...");

            ticker.Tick += Game_Tick;

            ticker.Run();

            Console.ReadKey();
            return;
        }

        private static void Game_Tick(Ticker ticker)
        {
            tickCount++;
            if (tickCount % 20 == 0) // Every second
            {
                manager.TickAlive(tickCount);

                //Console.WriteLine(
                //    "Tick Time: " +
                //    sender.ElapsedTime.TotalMilliseconds.ToString("00.00") +
                //    "/" +
                //    sender.TargetTime.TotalMilliseconds.ToString("00") + " ms" +
                //    " | " +
                //    (sender.ElapsedTime.Ticks / (float)sender.TargetTime.Ticks * 100f).ToString("00.0") + "%");

                int connectionCount = manager.UpdateConnections();

                if (connectionCount > 0)
                    Console.WriteLine(connectionCount + " connections");
            }

            //world.Tick();
            manager.Orchestrator.RequestFlush();
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

                connection.Close(immediate: false);
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
                var uuid = new UUID(0, 1);

                var name = loginStart.Name;
                var answer = new ServerLoginSuccess(uuid.ToUtf8String(), name);

                connection.EnqueuePacket(answer);

                connection.ProtocolState = ProtocolState.Play;

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
                // TODO broadcast to everyone
                Console.WriteLine("<" + connection.UserName + ">: " + chat.Message);
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
