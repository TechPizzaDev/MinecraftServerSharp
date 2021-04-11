using System;
using System.Collections.Generic;

namespace MCServerSharp.Blocks
{
    public class EnumStateProperty<TEnum> : StateProperty<TEnum>
        where TEnum : struct, Enum
    {
        private static TEnum[] Values { get; } = Enum.GetValues<TEnum>();

        public static ReadOnlyMemoryCharComparer KeyComparer { get; } = new(StringComparison.Ordinal);

        private Dictionary<TEnum, int> _valueToIndex;
        private Dictionary<ReadOnlyMemory<char>, int> _parseToIndex;
        
        public override int Count => _valueToIndex.Count;

        public EnumStateProperty(string name) : base(name)
        {
            _valueToIndex = new Dictionary<TEnum, int>(Values.Length);
            _parseToIndex = new Dictionary<ReadOnlyMemory<char>, int>(Values.Length, KeyComparer);

            int index = 0;
            foreach (TEnum value in Values)
            {
                _valueToIndex.Add(value, index);
                _parseToIndex.Add(value.ToString().ToSnake().ToLowerInvariant().AsMemory(), index);
                index++;
            }
        }

        public override int GetIndex(ReadOnlyMemory<char> value)
        {
            return _parseToIndex[value];
        }

        public override int GetIndex(TEnum value)
        {
            return _valueToIndex[value];
        }

        public override TEnum GetValue(int index)
        {
            return Values[index];
        }
    }
}
