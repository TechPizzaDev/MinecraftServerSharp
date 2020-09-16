
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PlayerAbilities)]
    public readonly struct ServerPlayerAbilities
    {
        [PacketProperty(0)] public ServerAbilityFlags Flags { get; }
        [PacketProperty(1)] public float FlyingSpeed { get; }
        [PacketProperty(2)] public float FieldofViewModifier { get; }

        public ServerPlayerAbilities(
            ServerAbilityFlags flags, float flyingSpeed, float fieldofViewModifier)
        {
            Flags = flags;
            FlyingSpeed = flyingSpeed;
            FieldofViewModifier = fieldofViewModifier;
        }
    }
}
