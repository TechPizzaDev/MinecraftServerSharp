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
        [Mapping(State.Play, 0x0f)] ChatMessage,

        [Mapping(State.Play, 0x15)] WindowProperty,
        [Mapping(State.Play, 0x18)] PluginMessage,
        [Mapping(State.Play, 0x1a)] PlayDisconnect,
        [Mapping(State.Play, 0x1d)] UnloadChunk,

        [Mapping(State.Play, 0x21)] KeepAlive,
        [Mapping(State.Play, 0x22)] ChunkData,
        [Mapping(State.Play, 0x25)] UpdateLight,
        [Mapping(State.Play, 0x26)] JoinGame,
        [Mapping(State.Play, 0x2b)] EntityRotation,
        [Mapping(State.Play, 0x2e)] OpenWindow,

        [Mapping(State.Play, 0x32)] PlayerAbilities,
        [Mapping(State.Play, 0x38)] PlayerPositionLook,

        [Mapping(State.Play, 0x49)] UpdateViewPosition,
        [Mapping(State.Play, 0x4a)] UpdateViewDistance,
        [Mapping(State.Play, 0x4b)] SpawnPosition,

        [Mapping(State.Play, 0x62)] EntityTeleport,
        [Mapping(State.Play, 0x67)] UpdateTags,

        #endregion
    }
}
