using Mapping = MinecraftServerSharp.Net.Packets.PacketIdMappingAttribute;
using State = MinecraftServerSharp.Net.Packets.ProtocolState;

namespace MinecraftServerSharp.Net.Packets
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

        [Mapping(State.Play, 0x03)] SpawnLivingEntity,
        [Mapping(State.Play, 0x0f)] ChatMessage,
        [Mapping(State.Play, 0x19)] PluginMessage,
        [Mapping(State.Play, 0x1b)] PlayDisconnect,

        [Mapping(State.Play, 0x21)] KeepAlive,
        [Mapping(State.Play, 0x22)] ChunkData,
        [Mapping(State.Play, 0x26)] JoinGame,

        [Mapping(State.Play, 0x36)] PlayerPositionLook,
        [Mapping(State.Play, 0x4E)] SpawnPosition,
        [Mapping(State.Play, 0x57)] EntityTeleport,

        #endregion
    }
}
