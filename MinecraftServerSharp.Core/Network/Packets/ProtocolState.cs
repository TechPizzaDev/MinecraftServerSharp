
namespace MinecraftServerSharp.Network.Packets
{
    public enum ProtocolState
    {
        Undefined = 0,

        Status = 1,
        Login = 2,

        Handshaking,
        Play,

        Disconnected
    }
}
