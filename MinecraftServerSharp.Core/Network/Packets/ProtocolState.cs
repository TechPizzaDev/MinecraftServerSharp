
namespace MinecraftServerSharp.Network.Packets
{
    public enum ProtocolState
    {
        Undefined,

        Handshaking,
        Status,
        Login,
        Play,

        Disconnected
    }
}
