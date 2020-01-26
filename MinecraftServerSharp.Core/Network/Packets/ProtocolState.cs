
namespace MinecraftServerSharp.Network.Packets
{
    public enum ProtocolState
    {
        Undefined = 0,

        Handshaking = 1,
        Status = 2,
        Login = 3,
        Play = 4,

        Disconnected = 5,

        /// <summary>
        /// Represents the maximum integer value of this enum.
        /// </summary>
        MAX = 6
    }
}
