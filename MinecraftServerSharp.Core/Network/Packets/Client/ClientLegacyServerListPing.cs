using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.LegacyServerListPing)]
    public readonly struct ClientLegacyServerListPing
    {
        public readonly byte PluginIdentifier;
        public readonly string MagicString;
        public readonly short DataLength;
        public readonly byte ProtocolVersion;
        public readonly string Hostname;
        public readonly int Port;

        [PacketConstructor]
        public ClientLegacyServerListPing(NetBinaryReader reader, out ReadCode code) : this()
        {
            code = reader.Read(out PluginIdentifier);
            if (code != ReadCode.Ok) 
                return;

            code = reader.Read(out short magicStringLength);
            if (code != ReadCode.Ok) 
                return;

            if (magicStringLength != 11)
            {
                code = ReadCode.InvalidData;
                return;
            }

            code = reader.Read(magicStringLength, out MagicString);
            if (code != ReadCode.Ok) 
                return;

            code = reader.Read(out DataLength);
            if (code != ReadCode.Ok) 
                return;

            code = reader.Read(out ProtocolVersion);
            if (code != ReadCode.Ok) 
                return;

            code = reader.Read(out short hostnameLength);
            if (code != ReadCode.Ok) 
                return;

            if (!StringHelper.IsValidStringLength(hostnameLength))
            {
                code = ReadCode.InvalidData;
                return;
            }

            code = reader.Read(hostnameLength, out Hostname);
            if (code != ReadCode.Ok)
                return;

            code = reader.Read(out Port);
        }
    }
}
