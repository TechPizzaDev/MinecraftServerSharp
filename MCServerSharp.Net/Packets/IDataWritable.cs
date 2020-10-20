using MCServerSharp.Data.IO;

namespace MCServerSharp.Net.Packets
{
    public interface IDataWritable
    {
        void Write(NetBinaryWriter writer);
    }
}
