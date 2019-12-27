using MinecraftServerSharp.Network.Packets;

namespace MinecraftServerSharp.Network
{
    public partial class NetProcessor
    {
        [PacketStruct(ClientPacketID.LegacyServerListPing, ProtocolState.Handshaking)]
        public readonly struct ClientLegacyServerListPing
        {
            [PacketProperty(0)]
            public byte Payload { get; }

            [PacketProperty(1)]
            public byte PluginIdentifier { get; }

            [PacketProperty(2)]
            public short MagicStringLength { get; }

            [PacketProperty(3, TextEncoding = NetTextEncoding.BigUtf16)]
            [PacketPropertyLength(nameof(MagicStringLength))]
            public string MagicString { get; }

            [PacketProperty(4)]
            public short DataLength { get; }

            [PacketProperty(5)]
            public byte ProtocolVersion { get; }

            [PacketProperty(6)]
            public short HostnameLength { get; }

            [PacketProperty(7, TextEncoding = NetTextEncoding.BigUtf16)]
            [PacketPropertyLength(nameof(HostnameLength))]
            public string Hostname { get; }

            [PacketProperty(8)]
            public int Port { get; }

            [PacketConstructor]
            public ClientLegacyServerListPing(
                byte payload, 
                byte pluginIdentifier, 
                short magicStringLength, 
                string magicString, 
                short dataLength, 
                byte protocolVersion, 
                short hostnameLength, 
                string hostname,
                int port)
            {
                Payload = payload;
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
