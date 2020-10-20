
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerAbilities)]
    public readonly struct ClientPlayerAbilities
    {
        public PlayerAbilityFlags Flags { get; }

        [PacketConstructor]
        public ClientPlayerAbilities(byte flags)
        {
            Flags = (PlayerAbilityFlags)flags;
        }
    }
}
