
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.EntityRotation)]
    public readonly struct ServerEntityRotation
    {
        [DataProperty(0)] public VarInt EntityId { get; }
        [DataProperty(1)] public Angle Yaw { get; }
        [DataProperty(2)] public Angle Pitch { get; }
        [DataProperty(3)] public bool OnGround { get; }

        public ServerEntityRotation(VarInt entityId, Angle yaw, Angle pitch, bool onGround)
        {
            EntityId = entityId;
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }
    }
}
