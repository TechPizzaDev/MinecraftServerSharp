
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerPosition)]
    public readonly struct ClientPlayerPosition
    {
        public double X { get; }
        public double FeetY { get; }
        public double Z { get; }
        public bool OnGround { get; }

        [PacketConstructor]
        public ClientPlayerPosition(double x, double feetY, double z, bool onGround)
        {
            X = x;
            FeetY = feetY;
            Z = z;
            OnGround = onGround;
        }
    }
}
