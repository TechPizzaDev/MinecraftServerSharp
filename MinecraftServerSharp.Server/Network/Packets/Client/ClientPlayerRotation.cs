
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerRotation)]
    public readonly struct ClientPlayerRotation
    {
        public float Yaw { get; }
        public float Pitch { get; }
        public bool OnGround { get; }

        [PacketConstructor]
        public ClientPlayerRotation(float yaw, float pitch, bool onGround)
        {
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }
    }
}
