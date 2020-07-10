using System;

namespace MinecraftServerSharp.Network.Packets
{

    [PacketStruct(ServerPacketId.JoinGame)]
    public readonly struct ServerJoinGame
    {
        [PacketProperty(0)] public int EntityId { get; }
        [PacketProperty(1)] public byte Gamemode { get; }
        [PacketProperty(2)] public int Dimension { get; }
        [PacketProperty(3)] public long HashedSeed { get; }
        [PacketProperty(4)] public byte MaxPlayers { get; }
        [PacketProperty(5), StringEnum] public Utf8String LevelType { get; }
        [PacketProperty(6)] public VarInt ViewDistance { get; }
        [PacketProperty(7)] public bool ReducedDebugInfo { get; }
        [PacketProperty(8)] public bool EnableRespawnScreen { get; }

        public ServerJoinGame(
            int entityId,
            byte gamemode, 
            int dimension, 
            long hashedSeed,
            byte maxPlayers, 
            Utf8String levelType, 
            VarInt viewDistance,
            bool reducedDebugInfo, 
            bool enableRespawnScreen)
        {
            LevelType = levelType ?? throw new ArgumentNullException(nameof(levelType));
            if (LevelType.Length > 16)
                throw new ArgumentException("Invalid length.", nameof(levelType));;

            if (viewDistance < 2 || viewDistance > 32)
                throw new ArgumentOutOfRangeException(nameof(viewDistance));

            EntityId = entityId;
            Gamemode = gamemode;
            Dimension = dimension;
            HashedSeed = hashedSeed;
            MaxPlayers = maxPlayers;

            ViewDistance = viewDistance;
            ReducedDebugInfo = reducedDebugInfo;
            EnableRespawnScreen = enableRespawnScreen;
        }
    }
}
