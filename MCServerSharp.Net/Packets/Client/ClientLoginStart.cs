using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.LoginStart)]
    public readonly struct ClientLoginStart
    {
        public Utf8String Name { get; }

        [PacketConstructor]
        public ClientLoginStart(
            [DataLengthConstraint(Max = 16)] Utf8String name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
