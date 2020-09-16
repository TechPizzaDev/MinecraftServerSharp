using System.IO;

namespace MCServerSharp
{
    public interface ISeekable
    {
        long Position { get; }
        long Length { get; }

        long Seek(int offset, SeekOrigin origin);
    }
}
