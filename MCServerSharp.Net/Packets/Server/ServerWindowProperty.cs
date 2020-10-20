
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.WindowProperty)]
    public readonly struct ServerWindowProperty
    {
        [DataProperty(0)] public byte WindowID { get; }
        [DataProperty(1)] public short Property { get; }
        [DataProperty(2)] public short Value { get; }

        public ServerWindowProperty(byte windowID, short property, short value)
        {
            WindowID = windowID;
            Property = property;
            Value = value;
        }
    }
}
