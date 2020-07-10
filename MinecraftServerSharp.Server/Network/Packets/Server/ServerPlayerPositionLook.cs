using System;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ServerPacketId.PlayerPositionLook)]
    public readonly struct ServerPlayerPositionLook
    {
        [Flags]
        public enum PositionRelatives : byte
        {
            None = 0,
            X = 0x01,
            Y = 0x02,
            Z = 0x04,
            Y_ROT = 0x08,
            X_ROT = 0x10,
        }

        [PacketProperty(0)] public double X { get; }
        [PacketProperty(1)] public double Y { get; }
        [PacketProperty(2)] public double Z { get; }
        [PacketProperty(3)] public float Yaw { get; }
        [PacketProperty(4)] public float Pitch { get; }
        [PacketProperty(5)] public PositionRelatives Flags { get; }
        [PacketProperty(6)] public VarInt TeleportId { get; }

        public ServerPlayerPositionLook(
            double x, double y, double z,
            float yaw, float pitch,
            PositionRelatives flags, VarInt teleportId)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            Flags = flags;
            TeleportId = teleportId;
        }
    }
}
