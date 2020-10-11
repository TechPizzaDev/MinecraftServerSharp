
namespace MCServerSharp.Blocks
{
    public class StatePropertyValue<T> : StatePropertyValue, IStateProperty<T>
    {
        public new IStateProperty<T> Property => (IStateProperty<T>)base.Property;

        public int ValueCount => Property.ValueCount;

        public StatePropertyValue(IStateProperty<T> property, int valueIndex) : base(property, valueIndex)
        {
        }

        public int GetIndex(T value)
        {
            return Property.GetIndex(value);
        }

        public T GetValue(int index)
        {
            return Property.GetValue(index);
        }

        public T GetValue()
        {
            return GetValue(ValueIndex);
        }

        public override string ToString()
        {
            return Name + "=" + GetValue();
        }
    }
}
