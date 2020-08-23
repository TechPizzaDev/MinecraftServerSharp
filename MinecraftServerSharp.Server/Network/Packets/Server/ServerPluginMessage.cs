namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ServerPacketId.PluginMessage)]
    public readonly struct ServerPluginMessage
    {
        [PacketProperty(0)] public Utf8String Channel { get; }
        [PacketProperty(1)] public Utf8String Data { get; }

        public ServerPluginMessage(Utf8String channel, Utf8String data)
        {
            Channel = channel;
            Data = data;
        }
    }
}
