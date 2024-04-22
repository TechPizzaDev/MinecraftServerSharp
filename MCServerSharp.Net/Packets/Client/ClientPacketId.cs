﻿using Mapping = MCServerSharp.Net.Packets.PacketIdMappingAttribute;
using State = MCServerSharp.Net.Packets.ProtocolState;

namespace MCServerSharp.Net.Packets
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
        [Mapping(State.Play, 0x08)] ClickWindow,
        [Mapping(State.Play, 0x09)] CloseWindow,
        [Mapping(State.Play, 0x0a)] PluginMessage,
        [Mapping(State.Play, 0x0f)] KeepAlive,

        [Mapping(State.Play, 0x11)] PlayerPosition,
        [Mapping(State.Play, 0x12)] PlayerPositionRotation,
        [Mapping(State.Play, 0x13)] PlayerRotation,
        [Mapping(State.Play, 0x14)] PlayerMovement,
        [Mapping(State.Play, 0x19)] PlayerAbilities,
        [Mapping(State.Play, 0x1a)] PlayerDigging,
        [Mapping(State.Play, 0x1b)] EntityAction,
        [Mapping(State.Play, 0x1e)] SetRecipeBookState,
        [Mapping(State.Play, 0x1f)] SetDisplayedRecipe,

        [Mapping(State.Play, 0x25)] HeldItemChange,
        [Mapping(State.Play, 0x28)] CreativeInventoryAction,
        [Mapping(State.Play, 0x2c)] Animation,
        [Mapping(State.Play, 0x2e)] PlayerBlockPlacement,
        [Mapping(State.Play, 0x2f)] UseItem,

        #endregion
    }
}
