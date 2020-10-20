
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PlayerAbilities)]
    public readonly struct ServerPlayerAbilities
    {
        [DataProperty(0)] public PlayerAbilityFlags Flags { get; }
        [DataProperty(1)] public float FlyingSpeed { get; }
        [DataProperty(2)] public float FieldOfViewModifier { get; }

        public ServerPlayerAbilities(
            PlayerAbilityFlags flags, float flyingSpeed, float fieldOfViewModifier)
        {
            Flags = flags;
            FlyingSpeed = flyingSpeed;
            FieldOfViewModifier = fieldOfViewModifier;
        }
    }
}
