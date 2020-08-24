
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SpawnLivingEntity)]
    public readonly struct ServerSpawnLivingEntity
    {
        [PacketProperty(0)] public VarInt EntityId { get; }
        [PacketProperty(1)] public UUID EntityUUID { get; }
        [PacketProperty(2)] public VarInt Type { get; }
        [PacketProperty(3)] public double X { get; }
        [PacketProperty(4)] public double Y { get; }
        [PacketProperty(5)] public double Z { get; }
        [PacketProperty(6)] public Angle Yaw { get; }
        [PacketProperty(7)] public Angle Pitch { get; }
        [PacketProperty(8)] public Angle HeadPitch { get; }
        [PacketProperty(9)] public short VelocityX { get; }
        [PacketProperty(10)] public short VelocityY { get; }
        [PacketProperty(11)] public short VelocityZ { get; }

        [PacketConstructor]
        public ServerSpawnLivingEntity(
            VarInt entityId, UUID entityUUID, VarInt type, 
            double x, double y, double z, 
            Angle yaw, Angle pitch, Angle headPitch,
            short velocityX, short velocityY, short velocityZ)
        {
            EntityId = entityId;
            EntityUUID = entityUUID;
            Type = type;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            HeadPitch = headPitch;
            VelocityX = velocityX;
            VelocityY = velocityY;
            VelocityZ = velocityZ;
        }
    }
}
