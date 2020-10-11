using System;
using System.Diagnostics;

namespace MCServerSharp.Blocks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class StateProperty<T> : IStateProperty<T>
    {
        public string Name { get; }

        public abstract int ValueCount { get; }

        protected StateProperty(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public abstract int ParseIndex(string value);

        public abstract int GetIndex(T value); 

        public abstract T GetValue(int index);

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
