using System;
using MCServerSharp.NBT;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.JoinGame)]
    public readonly struct ServerJoinGame
    {
        [DataProperty(0)] public int EntityId { get; }
        [DataProperty(1)] public bool IsHardcore { get; }
        [DataProperty(2)] public byte Gamemode { get; }
        [DataProperty(3)] public sbyte PreviousGamemode { get; }

        [DataProperty(4)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public Utf8Identifier[] WorldNames { get; }

        [DataProperty(5)] public NbtCompound DimensionCodec { get; }
        [DataProperty(6)] public NbtCompound Dimension { get; }
        [DataProperty(7)] public Utf8String WorldName { get; }
        [DataProperty(8)] public long HashedSeed { get; }
        [DataProperty(9)] public VarInt MaxPlayers { get; }
        [DataProperty(10)] public VarInt ViewDistance { get; }
        [DataProperty(11)] public bool ReducedDebugInfo { get; }
        [DataProperty(12)] public bool EnableRespawnScreen { get; }
        [DataProperty(13)] public bool IsDebug { get; }
        [DataProperty(14)] public bool IsFlat { get; }

        public ServerJoinGame(
            int entityId,
            bool isHardcore,
            byte gamemode,
            sbyte previousGamemode,
            Utf8Identifier[] worldNames,
            NbtCompound dimensionCodec,
            NbtCompound dimension,
            Utf8String worldName,
            long hashedSeed,
            VarInt maxPlayers,
            VarInt viewDistance,
            bool reducedDebugInfo,
            bool enableRespawnScreen,
            bool isDebug,
            bool isFlat)
        {
            if (viewDistance < 2 || viewDistance > 32)
                throw new ArgumentOutOfRangeException(nameof(viewDistance));

            DimensionCodec = dimensionCodec ?? throw new ArgumentNullException(nameof(dimensionCodec));
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));
            WorldName = worldName ?? throw new ArgumentNullException(nameof(worldName));
            WorldNames = worldNames ?? throw new ArgumentNullException(nameof(worldNames));

            EntityId = entityId;
            IsHardcore = isHardcore;
            Gamemode = gamemode;
            PreviousGamemode = previousGamemode;
            HashedSeed = hashedSeed;
            MaxPlayers = maxPlayers;
            ViewDistance = viewDistance;
            ReducedDebugInfo = reducedDebugInfo;
            EnableRespawnScreen = enableRespawnScreen;
            IsDebug = isDebug;
            IsFlat = isFlat;
        }
    }
}
