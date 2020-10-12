
namespace MCServerSharp.Blocks
{
    public interface IStateProperty<T> : IStateProperty
    {
        int ValueCount { get; }

        int GetIndex(T value);

        T GetValue(int index);
    }
}
