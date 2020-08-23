
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.PlayerMovement)]
    public readonly struct ClientPlayerMovement
    {
        public bool OnGround { get; }

        [PacketConstructor]
        public ClientPlayerMovement(bool onGround)
        {
            OnGround = onGround;
        }
    }
}
