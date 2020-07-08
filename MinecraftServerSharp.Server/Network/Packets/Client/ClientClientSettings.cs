using System;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.ClientSettings)]
    public readonly struct ClientClientSettings
    {
        public string Locale { get; }
        public sbyte ViewDistance { get; }
        public VarInt ChatMode { get; }
        public bool ChatColors { get; }
        public byte DisplayedSkinParts { get; }
        public VarInt MainHand { get; }

        [PacketConstructor]
        public ClientClientSettings(
            [LengthConstraint(Max = 16)] string locale,
            sbyte viewDistance, 
            VarInt chatMode, 
            bool chatColors, 
            byte displayedSkinParts,
            VarInt mainHand)
        {
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            ViewDistance = viewDistance;
            ChatMode = chatMode;
            ChatColors = chatColors;
            DisplayedSkinParts = displayedSkinParts;
            MainHand = mainHand;
        }
    }
}
