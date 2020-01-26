using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to delegates that turn packets into network messages.
    /// </summary>
    public partial class NetPacketEncoder : NetPacketCoder<ServerPacketID>
    {
        public delegate void PacketWriterDelegate<TPacket>(NetBinaryWriter writer, TPacket packet);

        public NetPacketEncoder() : base()
        {
            RegisterDataTypes();
        }

        #region RegisterDataType[s]

        protected override void RegisterDataType(params Type[] arguments)
        {
            RegisterDataTypeFromMethod(typeof(NetBinaryWriter), "Write", arguments);
        }

        protected virtual void RegisterDataTypes()
        {
            RegisterDataType(typeof(bool));
            RegisterDataType(typeof(sbyte));
            RegisterDataType(typeof(byte));
            RegisterDataType(typeof(short));
            RegisterDataType(typeof(ushort));
            RegisterDataType(typeof(int));
            RegisterDataType(typeof(long));

            RegisterDataType(typeof(VarInt));
            RegisterDataType(typeof(VarLong));

            RegisterDataType(typeof(float));
            RegisterDataType(typeof(double));

            RegisterDataType(typeof(Utf8String));
            RegisterDataType(typeof(string));
        }

        #endregion

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsServerPacket);
        }

        public PacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (PacketWriterDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override Delegate CreateCoderDelegate(PacketStructInfo structInfo)
        {
            var expressions = new List<Expression>();
            var writerParam = Expression.Parameter(typeof(NetBinaryWriter), "Writer");
            var packetParam = Expression.Parameter(structInfo.Type, "Packet");

            if (typeof(IWritablePacket).IsAssignableFrom(structInfo.Type))
            {
                string methodName = nameof(IWritablePacket.Write);
                var writeMethod = structInfo.Type.GetMethod(methodName, new[] { writerParam.Type });
                var writeCall = Expression.Call(packetParam, writeMethod, writerParam);
                expressions.Add(writeCall);
            }
            else
            {
                CreateComplexPacketWriter(expressions, writerParam, packetParam);
            }

            var writerDelegate = typeof(PacketWriterDelegate<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(expressions);
            var lambdaArgs = new[] { writerParam, packetParam };
            var resultLambda = Expression.Lambda(writerDelegate, lambdaBody, lambdaArgs);
            return resultLambda.Compile();
        }

        private void CreateComplexPacketWriter(
            List<Expression> expressions,
            ParameterExpression packetParam,
            ParameterExpression writerParam)
        {
            var publicProperties = packetParam.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var packetProperties = publicProperties.Select(property =>
            {
                var propertyAttribute = property.GetCustomAttribute<PacketPropertyAttribute>();
                if (propertyAttribute == null)
                    return null;

                var lengthConstraintAttrib = property.GetCustomAttribute<LengthConstraintAttribute>();
                return new PacketPropertyInfo(property, propertyAttribute, lengthConstraintAttrib);
            });

            var packetPropertyList = packetProperties.Where(x => x != null).ToList();
            packetPropertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            for (int i = 0; i < packetPropertyList.Count; i++)
            {
                var property = packetPropertyList[i];
                var propertyAccessor = Expression.Property(packetParam, property.Property);
                
                var writeMethod = DataTypes[new DataTypeKey(typeof(void), new[] { property.Type })];
                var args = new[] { propertyAccessor };
                
                var lengthPrefixedAttrib = writeMethod.GetCustomAttribute<LengthPrefixedAttribute>();
                if (lengthPrefixedAttrib != null)
                {
                    var lengthProperty = Expression.Property(propertyAccessor, "Length");
                    args = new[] { propertyAccessor, lengthProperty };
                }

                var call = Expression.Call(writerParam, writeMethod, args);
                expressions.Add(call);
            }

        }
    }
}
