using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.Handshake, ProtocolState.Handshaking)]
    public readonly struct ClientHandshake
    {
        public VarInt32 ProtocolVersion { get; }
        public Utf8String ServerAddress { get; }
        public ushort ServerPort { get; }
        public ProtocolState NextState { get; }

        [PacketConstructor]
        public ClientHandshake(
            VarInt32 protocolVersion,
            [LengthConstraint(Max = 255)] Utf8String serverAddress,
            ushort serverPort,
            VarInt32 nextState)
        {
            ProtocolVersion = protocolVersion;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            NextState = nextState.AsEnum<ProtocolState>();
        }
    }
}

