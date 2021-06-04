using System;
using System.Collections.Generic;
using MCServerSharp.Collections;

namespace MCServerSharp.Blocks
{
    public class EnumStateProperty<TEnum> : StateProperty<TEnum>
        where TEnum : struct, Enum
    {
        private static TEnum[] Values { get; } = Enum.GetValues<TEnum>();

        public static ReadOnlyMemoryCharComparer KeyComparerUtf16 { get; } = new(StringComparison.Ordinal);
        
        private Dictionary<TEnum, int> _valueToIndex;
        private Dictionary<ReadOnlyMemory<char>, int> _parseToIndex;
        private Dictionary<Utf8Memory, int> _parseToIndexUtf8;

        public override int Count => _valueToIndex.Count;

        public EnumStateProperty(string name) : base(name)
        {
            _valueToIndex = new Dictionary<TEnum, int>(Values.Length);
            _parseToIndex = new Dictionary<ReadOnlyMemory<char>, int>(Values.Length, KeyComparerUtf16);
            _parseToIndexUtf8 = new Dictionary<Utf8Memory, int>(Values.Length);

            int index = 0;
            foreach (TEnum value in Values)
            {
                _valueToIndex.Add(value, index);

                string valueName = value.ToString().ToSnake().ToLowerInvariant();
                _parseToIndex.Add(valueName.AsMemory(), index);
                _parseToIndexUtf8.Add(valueName.ToUtf8String(), index);
                index++;
            }
        }

        public override int GetIndex(ReadOnlyMemory<char> value)
        {
            return _parseToIndex[value];
        }

        public override int GetIndex(Utf8Memory value)
        {
            return _parseToIndexUtf8[value];
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
