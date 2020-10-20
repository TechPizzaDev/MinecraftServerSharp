namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PluginMessage)]
    public readonly struct ServerPluginMessage
    {
        [DataProperty(0)] public Utf8String Channel { get; }
        [DataProperty(1)] public Utf8String Data { get; }

        public ServerPluginMessage(Utf8String channel, Utf8String data)
        {
            Channel = channel;
            Data = data;
        }
    }
}
