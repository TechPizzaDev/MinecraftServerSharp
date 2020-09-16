
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerAbilities)]
    public readonly struct ClientPlayerAbilities
    {
        public byte Flags { get; }
        public float FlyingSpeed { get; }
        public float WalkingSpeed { get; }

        [PacketConstructor]
        public ClientPlayerAbilities(byte flags, float flyingSpeed, float walkingSpeed)
        {
            Flags = flags;
            FlyingSpeed = flyingSpeed;
            WalkingSpeed = walkingSpeed;
        }
    }
}
