using System.IO;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
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
        public ClientLegacyServerListPing(NetBinaryReader reader)
        {
            PluginIdentifier = reader.ReadByte();
            
            MagicStringLength = reader.ReadShort();
            if (MagicStringLength != 11) 
                throw new InvalidDataException();
            MagicString = reader.ReadUtf16String(MagicStringLength);

            DataLength = reader.ReadShort();
            ProtocolVersion = reader.ReadByte();

            HostnameLength = reader.ReadShort();
            NetTextHelper.AssertValidStringLength(HostnameLength);
            Hostname = reader.ReadUtf16String(HostnameLength);

            Port = reader.ReadInt();
        }
    }
}
