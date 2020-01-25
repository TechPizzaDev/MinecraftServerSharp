using System.IO;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.LegacyServerListPing)]
    public readonly struct ClientLegacyServerListPing
    {
        public byte PluginIdentifier { get; }
        public string MagicString { get; }
        public short DataLength { get; }
        public byte ProtocolVersion { get; }
        public string Hostname { get; }
        public int Port { get; }

        [PacketConstructor]
        public ClientLegacyServerListPing(NetBinaryReader reader, out ReadCode code) : this()
        {
            PluginIdentifier = reader.TryRead();
            
            var magicStringLength = reader.ReadShort();
            if (magicStringLength != 11)
            {
                code = ReadCode.InvalidData;
                return;
            }
            MagicString = reader.ReadUtf16String(magicStringLength);

            DataLength = reader.ReadShort();
            ProtocolVersion = reader.TryRead();

            var hostnameLength = reader.ReadShort();
            if (!NetTextHelper.IsValidStringLength(hostnameLength))
            {
                code = ReadCode.InvalidData;
                return;
            }
            Hostname = reader.ReadUtf16String(hostnameLength);

            Port = reader.ReadInt();

            code = ReadCode.Ok;
        }
    }
}
