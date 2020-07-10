
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.CloseWindow)]
    public readonly struct ClientCloseWindow
    {
        public byte WindowId { get; }

        [PacketConstructor]
        public ClientCloseWindow(byte windowId)
        {
            WindowId = windowId;
        }
    }
}
