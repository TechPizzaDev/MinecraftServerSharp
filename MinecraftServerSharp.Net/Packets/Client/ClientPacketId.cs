using Mapping = MinecraftServerSharp.Net.Packets.PacketIdMappingAttribute;
using State = MinecraftServerSharp.Net.Packets.ProtocolState;

namespace MinecraftServerSharp.Net.Packets
{
    public enum ClientPacketId
    {
        Undefined,

        #region Handshaking

        [Mapping(State.Handshaking, 0x00)] Handshake,
        [Mapping(State.Handshaking, 0xfe)] LegacyServerListPing,

        #endregion

        #region Status

        [Mapping(State.Status, 0x00)] Request,
        [Mapping(State.Status, 0x01)] Ping,

        #endregion

        #region Login

        [Mapping(State.Login, 0x00)] LoginStart,
        [Mapping(State.Login, 0x01)] EncryptionResponse,
        [Mapping(State.Login, 0x02)] LoginPluginResponse,

        #endregion

        #region Play

        [Mapping(State.Play, 0x00)] TeleportConfirm,
        [Mapping(State.Play, 0x03)] ChatMessage,
        [Mapping(State.Play, 0x05)] ClientSettings,
        [Mapping(State.Play, 0x0A)] CloseWindow,
        [Mapping(State.Play, 0x0B)] PluginMessage,
        
        [Mapping(State.Play, 0x0F)] KeepAlive,

        [Mapping(State.Play, 0x11)] PlayerPosition,
        [Mapping(State.Play, 0x12)] PlayerPositionRotation,
        [Mapping(State.Play, 0x13)] PlayerRotation,
        [Mapping(State.Play, 0x14)] PlayerMovement,
        [Mapping(State.Play, 0x19)] PlayerAbilities,

        [Mapping(State.Play, 0x1A)] PlayerDigging,
        [Mapping(State.Play, 0x1B)] EntityAction,
        [Mapping(State.Play, 0x1D)] RecipeBookData,

        [Mapping(State.Play, 0x2A)] Animation,
        [Mapping(State.Play, 0x23)] HeldItemChange,
        [Mapping(State.Play, 0x26)] CreativeInventoryAction,
        [Mapping(State.Play, 0x2C)] PlayerBlockPlacement,
        [Mapping(State.Play, 0x2D)] UseItem,

        #endregion
    }
}
