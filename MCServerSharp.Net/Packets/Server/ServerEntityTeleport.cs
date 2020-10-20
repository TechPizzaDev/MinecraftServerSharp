
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.EntityTeleport)]
    public readonly struct ServerEntityTeleport
    {
        [DataProperty(0)] public VarInt EntityId { get; }
        [DataProperty(1)] public double X { get; }
        [DataProperty(2)] public double Y { get; }
        [DataProperty(3)] public double Z { get; }
        [DataProperty(4)] public Angle Yaw { get; }
        [DataProperty(5)] public Angle Pitch { get; }
        [DataProperty(6)] public bool OnGround { get; }

        [PacketConstructor]
        public ServerEntityTeleport(
            VarInt entityId, double x, double y, double z, Angle yaw, Angle pitch, bool onGround)
        {
            EntityId = entityId;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }
    }
}
