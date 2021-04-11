namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.PluginMessage)]
    public readonly struct ServerPluginMessage
    {
        [DataProperty(0)] public Utf8Identifier Channel { get; }
        [DataProperty(1)] public Utf8String Data { get; }

        public ServerPluginMessage(Utf8Identifier channel, Utf8String data)
        {
            Channel = channel;
            Data = data;
        }
    }
}
