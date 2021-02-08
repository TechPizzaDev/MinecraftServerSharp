using MCServerSharp.Data.IO;

namespace MCServerSharp.Net.Packets
{
    public interface IDataWritable
    {
        void WriteTo(NetBinaryWriter writer);
    }
}
