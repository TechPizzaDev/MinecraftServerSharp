using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.Handshake)]
    public readonly struct ClientHandshake
    {
        public VarInt ProtocolVersion { get; }
        public Utf8String ServerAddress { get; }
        public ushort ServerPort { get; }
        public ProtocolState NextState { get; }

        [PacketConstructor]
        public ClientHandshake(
            VarInt protocolVersion,
            [LengthConstraint(Max = 255)] Utf8String serverAddress,
            ushort serverPort,
            VarInt nextState)
        {
            ProtocolVersion = protocolVersion;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            NextState = nextState.AsEnum<ProtocolState>();
        }
    }
}

