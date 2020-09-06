using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.EntityAction)]
    public readonly struct ClientEntityAction
    {
        public VarInt EntityId { get; }
        public ClientActionId ActionId { get; }
        public VarInt JumpBoost { get; }

        [PacketConstructor]
        public ClientEntityAction(VarInt entityId, VarInt actionId, VarInt jumpBoost)
        {
            EntityId = entityId;
            ActionId = (ClientActionId)actionId.Value;
            JumpBoost = jumpBoost;
        }
    }
}
