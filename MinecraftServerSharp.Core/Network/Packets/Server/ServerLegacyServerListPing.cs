using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    public readonly struct ServerLegacyServerListPing : INetWritable
    {
        public int ProtocolVersion { get; }
        public MinecraftVersion MinecraftVersion { get; }
        public string MessageOfTheDay { get; }
        public int CurrentPlayerCount { get; }
        public int MaxPlayers { get; }

        public void Write(NetBinaryWriter writer)
        {

        }
    }
}
