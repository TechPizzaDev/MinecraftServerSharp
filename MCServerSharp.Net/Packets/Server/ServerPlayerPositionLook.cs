using System;

namespace MCServerSharp.Net.Packets
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

        [DataProperty(0)] public double X { get; }
        [DataProperty(1)] public double Y { get; }
        [DataProperty(2)] public double Z { get; }
        [DataProperty(3)] public float Yaw { get; }
        [DataProperty(4)] public float Pitch { get; }
        [DataProperty(5)] public PositionRelatives Flags { get; }
        [DataProperty(6)] public VarInt TeleportId { get; }
        [DataProperty(7)] public bool DismountVehicle { get; }

        public ServerPlayerPositionLook(
            double x, double y, double z,
            float yaw, float pitch,
            PositionRelatives flags, 
            VarInt teleportId,
            bool dismountVehicle)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            Flags = flags;
            TeleportId = teleportId;
            DismountVehicle = dismountVehicle;
        }
    }
}
