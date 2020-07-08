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
    public partial class NetPacketEncoder : NetPacketCodec<ServerPacketId>
    {
        public delegate void PacketWriterDelegate<TPacket>(NetBinaryWriter writer, in TPacket packet);

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

            RegisterDataType(typeof(Chat));
            RegisterDataType(typeof(EntityId));
        }

        #endregion

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsServerPacket);
        }

        public PacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (PacketWriterDelegate<TPacket>)GetPacketCodec(typeof(TPacket));
        }

        protected override Delegate CreateCodecDelegate(PacketStructInfo structInfo)
        {
            var expressions = new List<Expression>();
            var writerParam = Expression.Parameter(typeof(NetBinaryWriter), "Writer");
            var packetParam = Expression.Parameter(structInfo.Type.MakeByRefType(), "Packet");

            if (typeof(IWritablePacket).IsAssignableFrom(structInfo.Type))
            {
                string methodName = nameof(IWritablePacket.Write);
                var writeMethod = structInfo.Type.GetMethod(methodName, new[] { writerParam.Type });
                var writeCall = Expression.Call(packetParam, writeMethod, writerParam);
                expressions.Add(writeCall);
            }
            else
            {
                CreateComplexPacketWriter(expressions, packetParam, writerParam);
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

            List<PacketPropertyInfo> packetPropertyList = packetProperties.Where(x => x != null).ToList()!;
            packetPropertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            for (int i = 0; i < packetPropertyList.Count; i++)
            {
                var propertyInfo = packetPropertyList[i];
                var property = Expression.Property(packetParam, propertyInfo.Property);

                var propertyTypeKey = DataTypeKey.FromVoid(propertyInfo.Type);
                if (!DataTypeHandlers.TryGetValue(propertyTypeKey, out var propertyWriteMethod))
                    throw new Exception("Missing write method \"" + propertyTypeKey + "\".");
                
                var lengthPrefixedAttrib = propertyWriteMethod.GetCustomAttribute<LengthPrefixedAttribute>();
                if (lengthPrefixedAttrib != null)
                {
                    throw new NotImplementedException();

                    var lengthProperty = Expression.Property(property, "Length");
                    var lengthWriteMethod = DataTypeHandlers[DataTypeKey.FromVoid(lengthPrefixedAttrib.LengthType)];
                    var lengthPropertyCast = Expression.Convert(lengthProperty, lengthPrefixedAttrib.LengthType);

                    expressions.Add(Expression.Call(writerParam, lengthWriteMethod, new[] { lengthPropertyCast }));
                }

                expressions.Add(Expression.Call(writerParam, propertyWriteMethod, new[] { property }));
            }
        }
    }
}
