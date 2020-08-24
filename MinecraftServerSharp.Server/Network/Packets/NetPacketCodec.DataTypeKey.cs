using System;
using System.Linq;

namespace MinecraftServerSharp.Net.Packets
{
    public abstract partial class NetPacketCodec<TPacketId>
        where TPacketId : Enum
    {
        public readonly struct DataTypeKey : IEquatable<DataTypeKey>
        {
            private readonly Type[] _parameters;

            public Type ReturnType { get; }
            public ReadOnlyMemory<Type> Parameters { get; }

            public DataTypeKey(Type returnType, params Type[] arguments)
            {
                ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
                _parameters = arguments ?? Array.Empty<Type>();
                Parameters = _parameters;
            }

            public static DataTypeKey FromVoid(params Type[] arguments)
            {
                return new DataTypeKey(typeof(void), arguments);
            }

            public bool Equals(DataTypeKey other)
            {
                return this == other;
            }

            public override bool Equals(object? obj)
            {
                return obj is DataTypeKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(ReturnType);

                for (int i = 0; i < _parameters.Length; i++)
                    hash.Add(_parameters[i]);

                return hash.ToHashCode();
            }

            public override string ToString()
            {
                return $"{ReturnType.Name} ({string.Join(", ", (object[])_parameters)})";
            }

            public static bool operator ==(
                NetPacketCodec<TPacketId>.DataTypeKey left, NetPacketCodec<TPacketId>.DataTypeKey right)
            {
                return left.ReturnType == right.ReturnType
                    && left._parameters.SequenceEqual(right._parameters);
            }

            public static bool operator !=(
                NetPacketCodec<TPacketId>.DataTypeKey left, NetPacketCodec<TPacketId>.DataTypeKey right)
            {
                return !(left == right);
            }
        }
    }
}
