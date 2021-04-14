using System;
using System.Diagnostics;

namespace MCServerSharp.Blocks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class StateProperty<T> : IStateProperty<T>
    {
        public string Name { get; }
        public Utf8String NameUtf8 { get; }

        public abstract int Count { get; }

        public Type ElementType => typeof(T);

        public StateProperty(string name, Utf8String nameUtf8)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NameUtf8 = nameUtf8 ?? throw new ArgumentNullException(nameof(nameUtf8));
        }

        public StateProperty(string name) : this(name, Utf8String.Create(name))
        {
        }

        public abstract int GetIndex(ReadOnlyMemory<char> value);

        public abstract int GetIndex(T value);

        public abstract T GetValue(int index);

        public StatePropertyValue<T> GetPropertyValue(int index)
        {
            return new StatePropertyValue<T>(this, index);
        }

        StatePropertyValue IStateProperty.GetPropertyValue(int index)
        {
            return new StatePropertyValue(this, index);
        }

        public override string ToString()
        {
            return Name + ": " + typeof(T).Name;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
