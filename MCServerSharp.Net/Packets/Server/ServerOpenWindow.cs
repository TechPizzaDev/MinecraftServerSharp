
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.OpenWindow)]
    public readonly struct ServerOpenWindow
    {
        [DataProperty(0)] public VarInt WindowId { get; }
        [DataProperty(1)] public VarInt WindowType { get; }
        [DataProperty(2)] public Chat WindowTitle { get; }

        public ServerOpenWindow(VarInt windowId, VarInt windowType, Chat windowTitle)
        {
            WindowId = windowId;
            WindowType = windowType;
            WindowTitle = windowTitle;
        }
    }
}
