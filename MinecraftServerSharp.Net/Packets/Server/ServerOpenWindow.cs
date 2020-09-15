
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.OpenWindow)]
    public readonly struct ServerOpenWindow
    {
        [PacketProperty(0)] public VarInt WindowID { get; }
        [PacketProperty(1)] public VarInt WindowType { get; }
        [PacketProperty(2)] public Chat WindowTitle { get; }

        public ServerOpenWindow(VarInt windowID, VarInt windowType, Chat windowTitle)
        {
            WindowID = windowID;
            WindowType = windowType;
            WindowTitle = windowTitle;
        }
    }
}
