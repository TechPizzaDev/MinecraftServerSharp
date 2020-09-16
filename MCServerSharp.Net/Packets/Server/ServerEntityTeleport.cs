
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.EntityTeleport)]
    public readonly struct ServerEntityTeleport
    {
        [PacketProperty(0)] public VarInt EntityId { get; }
        [PacketProperty(1)] public double X { get; }
        [PacketProperty(2)] public double Y { get; }
        [PacketProperty(3)] public double Z { get; }
        [PacketProperty(4)] public Angle Yaw { get; }
        [PacketProperty(5)] public Angle Pitch { get; }
        [PacketProperty(6)] public bool OnGround { get; }

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
