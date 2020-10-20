
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.OpenWindow)]
    public readonly struct ServerOpenWindow
    {
        [DataProperty(0)] public VarInt WindowID { get; }
        [DataProperty(1)] public VarInt WindowType { get; }
        [DataProperty(2)] public Chat WindowTitle { get; }

        public ServerOpenWindow(VarInt windowID, VarInt windowType, Chat windowTitle)
        {
            WindowID = windowID;
            WindowType = windowType;
            WindowTitle = windowTitle;
        }
    }
}
