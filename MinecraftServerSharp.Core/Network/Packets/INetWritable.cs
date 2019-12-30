using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    public interface INetWritable
    {
        void Write(NetBinaryWriter writer);
    }
}
