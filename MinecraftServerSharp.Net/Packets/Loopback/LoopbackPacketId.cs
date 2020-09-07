using Mapping = MinecraftServerSharp.Net.Packets.PacketIdMappingAttribute;
using State = MinecraftServerSharp.Net.Packets.ProtocolState;

namespace MinecraftServerSharp.Net.Packets
{
    public enum LoopbackPacketId
    {
        Undefined,
        [Mapping(State.Loopback, 0)] StateChange,
    }
}