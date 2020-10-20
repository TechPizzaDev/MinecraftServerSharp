
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SpawnLivingEntity)]
    public readonly struct ServerSpawnLivingEntity
    {
        [DataProperty(0)] public VarInt EntityId { get; }
        [DataProperty(1)] public UUID EntityUUID { get; }
        [DataProperty(2)] public VarInt Type { get; }
        [DataProperty(3)] public double X { get; }
        [DataProperty(4)] public double Y { get; }
        [DataProperty(5)] public double Z { get; }
        [DataProperty(6)] public Angle Yaw { get; }
        [DataProperty(7)] public Angle Pitch { get; }
        [DataProperty(8)] public Angle HeadPitch { get; }
        [DataProperty(9)] public short VelocityX { get; }
        [DataProperty(10)] public short VelocityY { get; }
        [DataProperty(11)] public short VelocityZ { get; }

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
