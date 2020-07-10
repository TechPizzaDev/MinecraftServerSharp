using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Data;
using MinecraftServerSharp.NBT;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to delegates that turn packets into network messages.
    /// </summary>
    public partial class NetPacketEncoder : NetPacketCodec<ServerPacketId>
    {
        public delegate void PacketWriterDelegate<TPacket>(NetBinaryWriter writer, in TPacket packet);

        private static Type[] _binaryWriterExtensions = new[]
        {
            typeof(NetBinaryWriter),
            typeof(NetBinaryWriterNbtExtensions),
            typeof(NetBinaryWriterTypeExtensions),
        };

        public NetPacketEncoder() : base()
        {
            RegisterDataTypes();
        }

        #region RegisterDataType[s]

        protected override void RegisterDataType(params Type[] arguments)
        {
            RegisterDataTypeFromMethod(_binaryWriterExtensions, "Write", arguments);
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
            RegisterDataType(typeof(Angle));
            RegisterDataType(typeof(Position));
            RegisterDataType(typeof(UUID));
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
            if (packetPropertyList.Count == 0)
            {
                Console.WriteLine($"Packet \"{packetParam.Type}\" has no properties.");
                return;
            }

            packetPropertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            for (int i = 0; i < packetPropertyList.Count; i++)
            {
                var propertyInfo = packetPropertyList[i];
                var property = Expression.Property(packetParam, propertyInfo.Property);
                bool isEnumProperty = propertyInfo.Type.IsEnum;

                MethodInfo? propertyWriteMethod;
                Type? writtenType;
                if (isEnumProperty)
                {
                    writtenType = propertyInfo.Type.GetEnumUnderlyingType();
                    var enumPropertyTypeKey = DataTypeKey.FromVoid(writtenType);
                    if (!DataTypeHandlers.TryGetValue(enumPropertyTypeKey, out propertyWriteMethod))
                        throw new Exception("Missing enum write method for \"" + enumPropertyTypeKey + "\".");
                }
                else
                {
                    writtenType = propertyInfo.Type;
                    var propertyTypeKey = DataTypeKey.FromVoid(writtenType);
                    if (!DataTypeHandlers.TryGetValue(propertyTypeKey, out propertyWriteMethod))
                        throw new Exception("Missing write method for \"" + propertyTypeKey + "\".");
                }

                var lengthPrefixedAttrib = propertyInfo.Property.GetCustomAttribute<LengthPrefixedAttribute>();
                if (lengthPrefixedAttrib != null)
                {
                    if (lengthPrefixedAttrib.LengthSource == LengthSource.CollectionLength)
                    {
                        var length = CollectionLength(property);
                        var lengthWriteMethod = DataTypeHandlers[DataTypeKey.FromVoid(lengthPrefixedAttrib.LengthType)];
                        var propertyLength = Expression.Convert(length, lengthPrefixedAttrib.LengthType);
                        expressions.Add(Expression.Call(writerParam, lengthWriteMethod, new[] { propertyLength }));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                // TODO: write collections

                var callArgument = isEnumProperty
                    ? Expression.Convert(property, writtenType)
                    : (Expression)property;

                expressions.Add(Expression.Call(writerParam, propertyWriteMethod, new[] { callArgument }));
            }
        }

        private static Expression CollectionLength(Expression instance)
        {
            if (instance.Type.GetGenericTypeDefinition() == typeof(ICollection<>))
                return Expression.Property(instance, typeof(ICollection<>).GetProperty("Count"));

            throw new Exception($"The expression is not of type {typeof(ICollection<>).Name}.");
        }
    }
}
