﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Data;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Enums;
using MCServerSharp.Maths;
using MCServerSharp.NBT;
using MCServerSharp.Net;
using MCServerSharp.Net.Packets;
using MCServerSharp.Utility;
using MCServerSharp.World;

namespace MCServerSharp.Runner
{
    // TODO: don't print anything for status connections
    //       (maybe print abnormal amount of pings from one address)

    public static class ServerMain
    {
        // TODO: move these to a Game class
        public const string PongResource = "Minecraft/Net/Pong.json";

        private static long _tickCount;
        private static NetManager _manager;
        private static string? _requestPongBase;

        private static Dictionary<Identifier, BlockDescription> _blockLookup;
        private static Dictionary<uint, BlockState> _stateLookup;
        private static BlockState[] _blockStates;
        private static DirectBlockPalette _directBlockPalette;

        private static Ticker _ticker;
        private static Dimension _mainDimension;

        private static List<NetConnection> _connectionsList = new List<NetConnection>();

        private static ConcurrentQueue<Player> _joiningPlayers = new ConcurrentQueue<Player>();
        private static ConcurrentQueue<Player> _leavingPlayers = new ConcurrentQueue<Player>();

        private static (Type, HashSet<string>)[] _stateEnumSets = new (Type, HashSet<string>)[]
        {
            GetEnumSet<Axis>(),
            GetEnumSet<HorizontalAxis>(),
            GetEnumSet<FacingDirection>(),
            GetEnumSet<FaceDirection>(),
            GetEnumSet<NoteInstrument>(),
            GetEnumSet<BedPartType>(),
            GetEnumSet<RailShape>(),
            GetEnumSet<RestrictedRailShape>(),
            GetEnumSet<TallGrassHalf>(),
            GetEnumSet<TallGrassType>(),
            GetEnumSet<DoubleFlowerType>(),
            GetEnumSet<PistonType>(),
            GetEnumSet<SlabType>(),
            GetEnumSet<StairHalf>(),
            GetEnumSet<StairShape>(),
            GetEnumSet<FaceType>(),
            GetEnumSet<SpecificFaceType>(),
            GetEnumSet<ChestType>(),
            GetEnumSet<DustConnection>(),
            GetEnumSet<Side>(),
            GetEnumSet<ComparatorMode>(),
            GetEnumSet<DownFaceDirection>(),
            GetEnumSet<BambooLeavesType>(),
            GetEnumSet<StructureBlockMode>(),
            GetEnumSet<WallExtension>(),
            GetEnumSet<Orientation>(),
        };

        private static void Main(string[] args)
        {
            // TODO: Create unit tests
            #region Look Testing
            //var q1 = Look.FromVectors(Vector3.Zero, new Vector3(0, 0, 1));
            //var q2 = Look.FromVectors(Vector3.Zero, new Vector3(1, 0, 0));
            //var q3 = Look.FromVectors(Vector3.Zero, new Vector3(0, 0, -1));
            //var q4 = Look.FromVectors(Vector3.Zero, new Vector3(-1, 0, 0));
            //var u1 = q1.ToUnitVector3();
            //var u2 = q2.ToUnitVector3();
            //var u3 = q3.ToUnitVector3();
            //var u4 = q4.ToUnitVector3();
            #endregion

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
                    Assembly.GetExecutingAssembly(), "MCServerSharp.Server.Templates.Config"));

            LoadGameData();

            _directBlockPalette = new DirectBlockPalette(_blockStates);
            _directBlockPalette.blockLookup = _blockLookup;
            _mainDimension = new Dimension(_directBlockPalette);

            using var pong = configProvider.OpenResourceReader(PongResource);
            if (pong == null)
                throw new KeyNotFoundException(PongResource);
            _requestPongBase = pong.ReadToEnd();

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

            // TODO: one thread per dimension

            _ticker = new Ticker(targetTickTime: TimeSpan.FromMilliseconds(50));
            _ticker.Tick += Game_Tick;
            _ticker.Run();

            Console.ReadKey();
        }

        private static void LoadGameData()
        {
            // TODO: create data generator tool (that runs before project build?) 
            // TODO: NET5 source generator for trivial access to blocks per supported version

            {
                Console.WriteLine("Loading blocks...");

                _blockLookup = LoadBlocks();
                _stateLookup = new Dictionary<uint, BlockState>();
                int stateCount = 0;
                uint maxStateId = 0;
                foreach (var block in _blockLookup.Values)
                {
                    stateCount += block.StateCount;
                    foreach (var state in block.GetStateSpan())
                    {
                        _stateLookup.Add(state.Id, state);
                        maxStateId = Math.Max(maxStateId, state.Id);
                    }
                }
                Console.WriteLine($"Loaded {_blockLookup.Count} blocks, {stateCount} states");

                _blockStates = new BlockState[maxStateId + 1];
                for (uint stateId = 0; stateId < _blockStates.Length; stateId++)
                {
                    if (!_stateLookup.TryGetValue(stateId, out var state))
                        throw new Exception("Missing state for Id " + stateId);
                    _blockStates[stateId] = state;
                }
            }
        }

        static (Type, HashSet<string>) GetEnumSet<TEnum>()
            where TEnum : struct, Enum
        {
            return (
                typeof(EnumStateProperty<TEnum>),
                new HashSet<string>(typeof(TEnum).GetEnumNames(), StringComparer.OrdinalIgnoreCase)
                );
        }

        private static IStateProperty ParseStateProperty(string name, List<string> values)
        {
            if (values.Count == 0)
                throw new ArgumentEmptyException(nameof(values));

            if (values.Count == 2)
            {
                if (values.Contains("true") && values.Contains("false"))
                    return new BooleanStateProperty(name);
            }

            bool isIntProperty = true;
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (string value in values)
            {
                if (!int.TryParse(value, out int intValue))
                {
                    isIntProperty = false;
                    break;
                }
                min = Math.Min(min, intValue);
                max = Math.Max(max, intValue);
            }
            if (isIntProperty)
                return new IntegerStateProperty(name, min, max);

            foreach (var (propertyType, enumValues) in _stateEnumSets)
            {
                if (enumValues.SetEquals(values))
                {
                    var enumProperty = Activator.CreateInstance(propertyType, name);
                    if (enumProperty == null)
                        throw new Exception("Failed to create enum property.");
                    return (IStateProperty)enumProperty;
                }
            }

            throw new Exception($"Missing enum for values \"{name}\": {{{values.ToListString()}}}");
        }

        private static Dictionary<Identifier, BlockDescription> LoadBlocks()
        {
            JsonDocument blocksDocument;
            using (var blocksFile = File.OpenRead("GameData/reports/blocks.json"))
                blocksDocument = JsonDocument.Parse(blocksFile);

            using (blocksDocument)
            {
                var blocksDictionary = new Dictionary<Identifier, BlockDescription>();
                var blockStatePropBuilder = new List<IStateProperty>();

                uint blockId = 0;
                foreach (var blockProperty in blocksDocument.RootElement.EnumerateObject())
                {
                    var blockName = new Identifier(blockProperty.Name);
                    var blockObject = blockProperty.Value;

                    var stateArray = blockObject.GetProperty("states");
                    int stateCount = stateArray.GetArrayLength();
                    int? defaultStateIndex = null;
                    for (int i = 0; i < stateCount; i++)
                    {
                        if (stateArray[i].TryGetProperty("default", out var defaultElement) &&
                            defaultElement.GetBoolean())
                        {
                            defaultStateIndex = i;
                            break;
                        }
                    }
                    if (defaultStateIndex == null)
                        Console.WriteLine(blockName + " is missing default state"); // TODO: print warning

                    static string GetEnumString(JsonElement element)
                    {
                        string? str = element.GetString();
                        if (str == null)
                            throw new ArgumentException("The element value as string is null.");
                        
                        // TODO: improve by converting to snake_case to PascalCase somewhere..
                        return str.Replace("_", "", StringComparison.Ordinal);
                    }

                    var blockProps = Array.Empty<IStateProperty>();
                    if (blockObject.TryGetProperty("properties", out var blockPropsObject))
                    {
                        blockStatePropBuilder.Clear();
                        foreach (var blockPropProp in blockPropsObject.EnumerateObject())
                        {
                            var propNames = blockPropProp.Value.EnumerateArray()
                                .Select(x => GetEnumString(x))
                                .ToList();

                            var parsedProp = ParseStateProperty(blockPropProp.Name, propNames);
                            blockStatePropBuilder.Add(parsedProp);
                        }
                        blockProps = blockStatePropBuilder.ToArray();
                    }

                    var blockStates = new BlockState[stateCount];
                    var block = new BlockDescription(
                        blockStates, blockProps,
                        blockName, blockId, defaultStateIndex.GetValueOrDefault());

                    for (int i = 0; i < blockStates.Length; i++)
                    {
                        var stateObject = stateArray[i];
                        var idProp = stateObject.GetProperty("id");
                        var propValues = Array.Empty<StatePropertyValue>();

                        if (blockProps.Length != 0)
                        {
                            propValues = new StatePropertyValue[blockProps.Length];
                            var statePropsObject = stateObject.GetProperty("properties");
                            int propertyIndex = 0;
                            foreach (var statePropProp in statePropsObject.EnumerateObject())
                            {
                                var blockStateProp = blockProps.First(x => statePropProp.NameEquals(x.Name));
                                int valueIndex = blockStateProp.ParseIndex(GetEnumString(statePropProp.Value));
                                propValues[propertyIndex++] = StatePropertyValue.Create(blockStateProp, valueIndex);
                            }
                        }
                        blockStates[i] = new BlockState(block, propValues, idProp.GetUInt32());
                    }

                    blocksDictionary.Add(blockName, block);
                    blockId++;
                }
                return blocksDictionary;
            }
        }

        private static void Game_Tick(Ticker ticker)
        {
            while (_joiningPlayers.TryDequeue(out var joining))
                _mainDimension.players.Add(joining);

            _connectionsList.Clear();
            _manager.UpdateConnections(_connectionsList, out int activeCount);

            while (_leavingPlayers.TryDequeue(out var leaving))
                _mainDimension.players.Remove(leaving);

            _tickCount++;
            if (_tickCount % 20 == 0) // Every second
            {
                //if (updateCount > 0)
                //    Console.WriteLine(activeCount + " connections");

                //Console.WriteLine(
                //    "Tick Time: " +
                //    ticker.ElapsedTime.TotalMilliseconds.ToString("00.0") +
                //    "/" +
                //    ticker.TargetTime.TotalMilliseconds.ToString("00") + " ms" +
                //    " | " +
                //    (ticker.ElapsedTime.Ticks / (float)ticker.TargetTime.Ticks * 100f).ToString("00") + "%");
            }

            _mainDimension.Tick();
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
                if (connection.ProtocolState == ProtocolState.Closing)
                    return;

                var setCompression = new ServerSetCompression(128);
                connection.EnqueuePacket(setCompression);
                connection.CompressionThreshold = setCompression.Threshold;

                var uuid = new UUID(0, 1);
                var name = loginStart.Name;
                var loginSuccess = new ServerLoginSuccess(uuid, name);

                var gameTimeComponent = new GameTimeComponent(_ticker);
                connection.Components.Add(gameTimeComponent);

                var player = new Player(_mainDimension)
                {
                    UserName = name.ToString(),
                    UserUUID = uuid,
                };
                player.Components.Add(gameTimeComponent);
                connection.Components.Add(new PlayerComponent(player));
                player.Components.Add(new NetConnectionComponent(connection));

                connection.EnqueuePacket(loginSuccess);
                connection.ProtocolState = ProtocolState.Play;
                _joiningPlayers.Enqueue(player);

                var playerId = new EntityId(69);

                var dimensionTag = new NbtCompound
                {
                    { "piglin_safe"         , new NbtByte(0) },
                    { "natural"             , new NbtByte(1) },
                    { "ambient_light"       , new NbtFloat(1f) },
                    { "infiniburn"          , new NbtString("minecraft:infiniburn_overworld") },
                    { "respawn_anchor_works", new NbtByte(1) },
                    { "has_skylight"        , new NbtByte(1) },
                    { "bed_works"           , new NbtByte(1) },
                    { "effects"             , new NbtString("minecraft:overworld") },
                    { "has_raids"           , new NbtByte(1) },
                    { "logical_height"      , new NbtInt(256) },
                    { "coordinate_scale"    , new NbtFloat(1f) },
                    { "ultrawarm"           , new NbtByte(0) },
                    { "has_ceiling"         , new NbtByte(0) },
                };

                connection.EnqueuePacket(new ServerJoinGame(
                    playerId.Value,
                    false,
                    1,
                    -1,
                    new Identifier[] { "minecraft:overworld" },
                    new NbtCompound
                    {
                        { "minecraft:dimension_type", new NbtCompound
                        {
                            { "type", new NbtString("minecraft:dimension_type") },
                            { "value", new NbtList<NbtCompound>
                            {
                                new NbtCompound
                                {
                                    { "name", new NbtString("minecraft:overworld") },
                                    { "id", new NbtInt(0) },
                                    { "element", dimensionTag }
                                }
                            }
                            }
                        }
                        },
                        { "minecraft:worldgen/biome", new NbtCompound
                        {
                            { "type", new NbtString("minecraft:worldgen/biome") },
                            { "value", new NbtList<NbtCompound>
                            {
                                new NbtCompound
                                {
                                    { "name", new NbtString("minecraft:plains") },
                                    { "id", new NbtInt(1) },
                                    { "element", new NbtCompound
                                    {
                                        { "precipitation", new NbtString("rain") },
                                        { "effects", new NbtCompound
                                        {
                                            { "sky_color", new NbtInt(7907327) },
                                            { "water_fog_color", new NbtInt(329011) },
                                            { "fog_color", new NbtInt(12638463) },
                                            { "water_color", new NbtInt(4159204) },
                                            { "mood_sound", new NbtCompound
                                            {
                                                { "tick_delay", new NbtInt(6000) },
                                                { "offset", new NbtDouble(2.0) },
                                                { "sound", new NbtString("minecraft:ambient.cave") },
                                                { "block_search_extent", new NbtInt(8) }
                                            }
                                            }
                                        }
                                        },
                                        { "depth"      , new NbtFloat (0.125f) },
                                        { "temperature", new NbtFloat (0.8f) },
                                        { "scale"      , new NbtFloat (0.05f) },
                                        { "downfall"   , new NbtFloat (0.4f) },
                                        { "category"   , new NbtString("plains") }
                                    }
                                    },
                                },
                            }
                            },
                        }
                        },
                    },
                    dimensionTag,
                    (Utf8String)"overworld",
                    0,
                    16,
                    32, // TODO: respect view distance in NetConnectionComponent
                    false,
                    true,
                    false,
                    true));

                connection.EnqueuePacket(new ServerPluginMessage(
                    (Utf8String)"minecraft:brand",
                    (Utf8String)"MCServerSharp"));

                connection.EnqueuePacket(new ServerSpawnPosition(
                    new Position(0, 7, 0)));

                connection.EnqueuePacket(new ServerPlayerPositionLook(
                    0, 7, 0, 0, 0, ServerPlayerPositionLook.PositionRelatives.None, 1337));

                connection.EnqueuePacket(new ServerPlayerAbilities(
                    PlayerAbilityFlags.AllowFlying | PlayerAbilityFlags.Flying,
                    0.8f, // 0.5f, // 0.33f,
                    0.1f));
            });


            static void PlayerPositionChange(NetConnection connection, double x, double y, double z)
            {
                Player player = connection.GetPlayer();

                player.Position = new Vector3d(x, y, z);

                //player.IntY = (int)Math.Round(player.Position.Y);
                //if (player.LastIntY != player.IntY)
                //{
                //    player.LastIntY = player.IntY;
                //
                //    connection.EnqueuePacket(
                //        new ServerUpdateViewPosition(
                //            player.ChunkPosition.X,
                //            player.ChunkPosition.Z));
                //}
                //
                //player.LastPosition = player.Position;
            }

            static void PlayerRotationChange(NetConnection connection, float yaw, float pitch)
            {

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
                    while (x < 40)
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
                (NetConnection connection, ClientSettings clientSettings)
            {
                // The player object should be created early in the pipeline.
                var component = connection.Components.GetOrAdd(
                    connection, (c) => new ClientSettingsComponent(c.GetPlayer()));

                component.Settings = clientSettings;
                component.SettingsChanged = true;

                Console.WriteLine("got client settings");
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
                (NetConnection connection, ClientSetRecipeBookState recipeBookData)
            {
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientChat chat)
            {
                // TODO: better broadcasting

                string? name = connection.GetPlayer().UserName;
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

                var chatToSend = new Chat(new Utf8String(JsonSerializer.SerializeToUtf8Bytes(dyn)));

                lock (manager.ConnectionMutex)
                {
                    foreach (var conn in manager.Connections)
                    {
                        conn.EnqueuePacket(new ServerChat(chatToSend, 0, conn.GetPlayer().UserUUID));
                    }
                }
            });


            manager.SetPacketHandler(delegate
                (NetConnection connection, ClientPluginMessage pluginMessage)
            {
                Console.WriteLine(
                    connection.GetPlayer().UserName + " - Plugin @ " + pluginMessage.Channel + ": \"" +
                    new Utf8String(pluginMessage.Data) + "\"");
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

            if (connection.GetPlayer(out Player? player))
                _leavingPlayers.Enqueue(player);
        }
    }
}
