
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerPositionRotation)]
    public readonly struct ClientPlayerPositionRotation
    {
        public double X { get; }
        public double FeetY { get; }
        public double Z { get; }
        public float Yaw { get; }
        public float Pitch { get; }
        public bool OnGround { get; }

        [PacketConstructor]
        public ClientPlayerPositionRotation(
            double x, double feetY, double z, float yaw, float pitch, bool onGround)
        {
            X = x;
            FeetY = feetY;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }
    }
}
