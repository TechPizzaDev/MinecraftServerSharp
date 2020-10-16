using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.ClientSettings)]
    public readonly struct ClientSettings : IEquatable<ClientSettings>
    {
        public Utf8String Locale { get; }
        public sbyte ViewDistance { get; }
        public VarInt ChatMode { get; }
        public bool ChatColors { get; }
        public byte DisplayedSkinParts { get; }
        public VarInt MainHand { get; }

        [PacketConstructor]
        public ClientSettings(
            [LengthConstraint(Max = 16)] Utf8String locale,
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

        public bool Equals(ClientSettings other)
        {
            // Ordered by randomly estimated typical difference.

            return ViewDistance == other.ViewDistance
                && Locale == other.Locale
                && DisplayedSkinParts == other.DisplayedSkinParts
                && MainHand == other.MainHand
                && ChatColors == other.ChatColors
                && ChatMode == other.ChatMode;
        }

        public override bool Equals(object? obj)
        {
            return obj is ClientSettings value && Equals(value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Locale, ViewDistance, ChatMode, ChatColors, DisplayedSkinParts, MainHand);
        }

        public static bool operator ==(ClientSettings left, ClientSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ClientSettings left, ClientSettings right)
        {
            return !(left == right);
        }
    }
}
