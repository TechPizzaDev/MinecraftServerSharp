
namespace MCServerSharp.Blocks
{
    public interface IStateProperty<T> : IStateProperty
    {
        int GetIndex(T value);
        T GetValue(int index);
        new StatePropertyValue<T> GetPropertyValue(int index);
    }
}
