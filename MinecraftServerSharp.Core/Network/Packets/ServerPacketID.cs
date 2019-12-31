
namespace MinecraftServerSharp.Network.Packets
{
    public enum ServerPacketID
    {
        Undefined,
        LegacyServerListPong,

        #region Status

        Response,
        Pong,

        #endregion

        #region Login

        Disconnect,
        EncryptionRequest,
        LoginSuccess,
        SetCompression,
        LoginPluginRequest,

        #endregion

        #region Play



        #endregion
    }
}
