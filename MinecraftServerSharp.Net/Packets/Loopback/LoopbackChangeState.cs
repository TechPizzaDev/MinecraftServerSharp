
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(LoopbackPacketId.StateChange)]
    public readonly struct LoopbackChangeState
    {
        [PacketProperty(0)]
        public ProtocolState NextState { get; }

        public LoopbackChangeState(ProtocolState nextState)
        {
            NextState = nextState;
        }

        [PacketConstructor()]
        public LoopbackChangeState(VarInt nextState) : this(nextState.AsEnum<ProtocolState>())
        {
        }
    }
}
