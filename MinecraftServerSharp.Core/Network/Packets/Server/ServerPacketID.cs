using Mapping = MinecraftServerSharp.Network.Packets.PacketIDMappingAttribute;
using State = MinecraftServerSharp.Network.Packets.ProtocolState;

namespace MinecraftServerSharp.Network.Packets
{
    public enum ServerPacketID
    {
        Undefined,
        [Mapping(0xff, State.Handshaking)] LegacyServerListPong,

        #region Status

        Response,
        Pong,

        #endregion

        #region Login

        LoginDisconnect,
        EncryptionRequest,
        LoginSuccess,
        SetCompression,
        LoginPluginRequest,

        #endregion

        #region Play

        [Mapping(0x0e, State.Play)] ChatMessage,
        [Mapping(0x1a, State.Play)] PlayDisconnect

        #endregion
    }
}
