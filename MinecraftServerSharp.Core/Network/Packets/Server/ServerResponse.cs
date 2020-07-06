
namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ServerPacketId.Response)]
    public readonly struct ServerResponse
    {
        [PacketProperty(0)] public Utf8String JsonResponse { get; }

        public ServerResponse(Utf8String jsonResponse)
        {
            JsonResponse = jsonResponse;
        }
    }
}
