using System.IO;

namespace MinecraftServerSharp
{
    public interface ISeekable
    {
        long Position { get; }
        long Length { get; }

        long Seek(int offset, SeekOrigin origin);
    }
}
