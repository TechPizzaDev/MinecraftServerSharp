using System;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.LoginStart)]
    public readonly struct ClientLoginStart
    {
        public Utf8String Name { get; }

        [PacketConstructor]
        public ClientLoginStart(
            [LengthConstraint(Max = 16)] Utf8String name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
