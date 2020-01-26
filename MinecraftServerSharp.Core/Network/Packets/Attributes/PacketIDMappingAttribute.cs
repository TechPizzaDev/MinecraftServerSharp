using System;

namespace MinecraftServerSharp.Network.Packets
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PacketIDMappingAttribute : Attribute
    {
        public int RawID { get; }
        public ProtocolState State { get; }

        public PacketIDMappingAttribute(int rawID, ProtocolState state)
        {
            switch (state)
            {
                case ProtocolState.Handshaking:
                case ProtocolState.Status:
                case ProtocolState.Login:
                case ProtocolState.Play:
                    break;

                default:
                case ProtocolState.Undefined:
                case ProtocolState.Disconnected:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }

            RawID = rawID;
            State = state;
        }
    }
}
