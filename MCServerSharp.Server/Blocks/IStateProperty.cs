
namespace MCServerSharp.Blocks
{
    public interface IStateProperty
    {
        string Name { get; }

        int ParseIndex(string value);
    }
}
