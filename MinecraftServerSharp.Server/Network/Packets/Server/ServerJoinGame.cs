using System;

namespace MinecraftServerSharp.Network.Packets
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class StringEnumAttribute : Attribute
    {
    }

    [PacketStruct(ServerPacketId.JoinGame)]
    public readonly struct ServerJoinGame
    {
        [PacketProperty(0)] public EntityId EntityId { get; }
        [PacketProperty(1)] public byte Gamemode { get; }
        [PacketProperty(2)] public int Dimension { get; }
        [PacketProperty(3)] public long HashedSeed { get; }
        [PacketProperty(4)] public byte MaxPlayers { get; }
        [PacketProperty(5), StringEnum] public Utf8String LevelType { get; }
        [PacketProperty(6)] public VarInt ViewDistance { get; }
        [PacketProperty(7)] public bool ReducedDebugInfo { get; }
        [PacketProperty(8)] public bool EnableRespawnScreen { get; }

        public ServerJoinGame(
            EntityId entityId,
            byte gamemode, 
            int dimension, 
            long hashedSeed,
            byte maxPlayers, 
            Utf8String levelType, 
            VarInt viewDistance,
            bool reducedDebugInfo, 
            bool enableRespawnScreen)
        {
            if (viewDistance < 2 || viewDistance > 32)
                throw new ArgumentOutOfRangeException(nameof(viewDistance));

            EntityId = entityId;
            Gamemode = gamemode;
            Dimension = dimension;
            HashedSeed = hashedSeed;
            MaxPlayers = maxPlayers;
            LevelType = levelType ?? throw new ArgumentNullException(nameof(levelType));
            ViewDistance = viewDistance;
            ReducedDebugInfo = reducedDebugInfo;
            EnableRespawnScreen = enableRespawnScreen;

            if (LevelType.Length > 16)
                throw new ArgumentException(nameof(levelType));
        }
    }
}
