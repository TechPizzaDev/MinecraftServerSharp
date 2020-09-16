
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.WindowProperty)]
    public readonly struct ServerWindowProperty
    {
        [PacketProperty(0)] public byte WindowID { get; }
        [PacketProperty(1)] public short Property { get; }
        [PacketProperty(2)] public short Value { get; }

        public ServerWindowProperty(byte windowID, short property, short value)
        {
            WindowID = windowID;
            Property = property;
            Value = value;
        }
    }
}
