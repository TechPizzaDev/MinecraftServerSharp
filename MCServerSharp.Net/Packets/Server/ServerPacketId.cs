using Mapping = MCServerSharp.Net.Packets.PacketIdMappingAttribute;
using State = MCServerSharp.Net.Packets.ProtocolState;

namespace MCServerSharp.Net.Packets
{
    public enum ServerPacketId
    {
        Undefined,

        [Mapping(State.Handshaking, 0xff)] LegacyServerListPong,

        #region Status

        [Mapping(State.Status, 0x00)] Response,
        [Mapping(State.Status, 0x01)] Pong,

        #endregion

        #region Login

        [Mapping(State.Login, 0x00)] LoginDisconnect,
        [Mapping(State.Login, 0x01)] EncryptionRequest,
        [Mapping(State.Login, 0x02)] LoginSuccess,
        [Mapping(State.Login, 0x03)] SetCompression,
        [Mapping(State.Login, 0x04)] LoginPluginRequest,

        #endregion

        #region Play

        [Mapping(State.Play, 0x02)] SpawnLivingEntity,
        [Mapping(State.Play, 0x0e)] ChatMessage,

        [Mapping(State.Play, 0x14)] WindowProperty,
        [Mapping(State.Play, 0x17)] PluginMessage,
        [Mapping(State.Play, 0x19)] PlayDisconnect,
        [Mapping(State.Play, 0x1f)] KeepAlive,

        [Mapping(State.Play, 0x20)] ChunkData,
        [Mapping(State.Play, 0x23)] UpdateLight,
        [Mapping(State.Play, 0x24)] JoinGame,
        [Mapping(State.Play, 0x2d)] OpenWindow,

        [Mapping(State.Play, 0x30)] PlayerAbilities,
        [Mapping(State.Play, 0x34)] PlayerPositionLook,

        [Mapping(State.Play, 0x40)] UpdateViewPosition,
        [Mapping(State.Play, 0x41)] UpdateViewDistance,
        [Mapping(State.Play, 0x42)] SpawnPosition,

        [Mapping(State.Play, 0x56)] EntityTeleport,

        #endregion
    }
}
