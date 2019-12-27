
namespace MinecraftServerSharp.Network.Packets
{
    public enum ClientPacketID
    {
        Undefined,

        #region Handshaking

        Handshake,
        LegacyServerListPing,

        #endregion

        #region Status

        Request,
        Ping,

        #endregion

        #region Login

        LoginStart,
        EncryptionResponse,
        LoginPluginResponse,

        #endregion

        #region Play



        #endregion
    }
}
