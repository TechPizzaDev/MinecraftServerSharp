using System;
using System.Diagnostics;

namespace MCServerSharp.Blocks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class StateProperty<T> : IStateProperty<T>
    {
        public string NameUtf16 { get; }
        public Utf8String Name { get; }

        public abstract int Count { get; }

        public Type ElementType => typeof(T);

        public StateProperty(string name, Utf8String nameUtf8)
        {
            NameUtf16 = name ?? throw new ArgumentNullException(nameof(name));
            Name = nameUtf8 ?? throw new ArgumentNullException(nameof(nameUtf8));
        }

        public StateProperty(string name) : this(name, Utf8String.Create(name))
        {
        }

        public abstract int GetIndex(ReadOnlyMemory<char> value);

        public abstract int GetIndex(Utf8Memory value);

        public abstract int GetIndex(T value);

        public abstract T GetValue(int index);

        public override string ToString()
        {
            return NameUtf16 + ": " + typeof(T).Name;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
