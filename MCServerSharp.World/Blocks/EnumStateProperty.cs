using System;
using System.Collections.Generic;

namespace MCServerSharp.Blocks
{
    public class EnumStateProperty<TEnum> : StateProperty<TEnum>
        where TEnum : struct, Enum
    {
        private Dictionary<TEnum, int> _valueToIndex;
        private Dictionary<string, int> _parseToIndex;
        private TEnum[] _values;

        public override int ValueCount => _valueToIndex.Count;

        public EnumStateProperty(string name) : base(name)
        {
            _values = (TEnum[])typeof(TEnum).GetEnumValues();
            _valueToIndex = new Dictionary<TEnum, int>(_values.Length);
            _parseToIndex = new Dictionary<string, int>(_values.Length);

            int index = 0;
            foreach (TEnum value in _values)
            {
                _valueToIndex.Add(value, index);
                _parseToIndex.Add(value.ToString().ToSnake().ToLowerInvariant(), index);
                index++;
            }
        }

        public override int ParseIndex(string value)
        {
            return _parseToIndex[value];
        }

        public override int GetIndex(TEnum value)
        {
            return _valueToIndex[value];
        }

        public override TEnum GetValue(int index)
        {
            return _values[index];
        }
    }
}
