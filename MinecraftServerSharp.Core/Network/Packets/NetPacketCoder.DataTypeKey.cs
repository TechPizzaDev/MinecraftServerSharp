using System;
using System.Linq;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder<TPacketID>
        where TPacketID : Enum
    {
        public readonly struct DataTypeKey : IEquatable<DataTypeKey>
        {
            public Type ReturnType { get; }
            public Type[] Parameters { get; }

            public DataTypeKey(Type returnType, Type[] arguments = null)
            {
                ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
                Parameters = arguments ?? Array.Empty<Type>();
            }

            public bool Equals(DataTypeKey other)
            {
                return ReturnType == other.ReturnType
                    && Parameters.SequenceEqual(other.Parameters);
            }

            public override bool Equals(object obj)
            {
                return obj is DataTypeKey key ? Equals(key) : false;
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(ReturnType);

                for (int i = 0; i < Parameters.Length; i++)
                    hash.Add(Parameters[i]);

                return hash.ToHashCode();
            }
        }
    }
}
