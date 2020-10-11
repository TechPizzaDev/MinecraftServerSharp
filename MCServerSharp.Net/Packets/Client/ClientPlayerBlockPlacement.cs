using MCServerSharp.Data;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerBlockPlacement)]
    public readonly struct ClientPlayerBlockPlacement
    {
        public HandId Hand { get; }
        public Position Location { get; }
        public FaceDirection Face { get; }
        public float CursorPositionX { get; }
        public float CursorPositionY { get; }
        public float CursorPositionZ { get; }
        public bool InsideBlock { get; }

        [PacketConstructor]
        public ClientPlayerBlockPlacement(
            VarInt hand,
            Position location,
            VarInt face,
            float cursorPositionX, 
            float cursorPositionY, 
            float cursorPositionZ, 
            bool insideBlock)
        {
            Hand = hand.AsEnum<HandId>();
            Location = location;
            Face = face.AsEnum<FaceDirection>();
            CursorPositionX = cursorPositionX;
            CursorPositionY = cursorPositionY;
            CursorPositionZ = cursorPositionZ;
            InsideBlock = insideBlock;
        }
    }
}
