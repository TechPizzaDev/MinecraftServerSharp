using System;

namespace MinecraftServerSharp.Net.Packets
{
    // TODO: add dynamic ID mapping by file

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PacketIdMappingAttribute : Attribute
    {
        public ProtocolState State { get; }
        public int RawId { get; }

        public PacketIdMappingAttribute(ProtocolState state, int rawId)
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

            State = state;
            RawId = rawId;
        }
    }
}
