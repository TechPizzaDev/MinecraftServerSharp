 using MinecraftServerSharp.Network.Packets;

namespace MinecraftServerSharp.Network
{
    public partial class NetProcessor
    {
        [PacketStruct(ClientPacketID.LegacyServerListPing, ProtocolState.Handshaking)]
        public readonly struct ClientLegacyServerListPing
        {
            public byte PluginIdentifier { get; }
            public short MagicStringLength { get; }
            public string MagicString { get; }
            public short DataLength { get; }
            public byte ProtocolVersion { get; }
            public short HostnameLength { get; }
            public string Hostname { get; }
            public int Port { get; }

            [PacketConstructor]
            public ClientLegacyServerListPing(
                byte pluginIdentifier,
                [LengthConstraint(Constant = 11)] short magicStringLength,
                [LengthFrom(-1)] string magicString, 
                short dataLength, 
                byte protocolVersion, 
                short hostnameLength,
                [LengthFrom(-1)] string hostname,
                int port)
            {
                PluginIdentifier = pluginIdentifier;
                MagicStringLength = magicStringLength;
                MagicString = magicString;
                DataLength = dataLength;
                ProtocolVersion = protocolVersion;
                HostnameLength = hostnameLength;
                Hostname = hostname;
                Port = port;
            }
        }
    }
}
