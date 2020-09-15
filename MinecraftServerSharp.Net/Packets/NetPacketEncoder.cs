using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.NBT;

namespace MinecraftServerSharp.Net.Packets
{
    public delegate void NetPacketWriterDelegate<TPacket>(
        NetBinaryWriter writer, in TPacket packet);

    /// <summary>
    /// Gives access to delegates that turn packets into network messages.
    /// </summary>
    public partial class NetPacketEncoder : NetPacketCoder<ServerPacketId>
    {
        private static Type[] _binaryWriterWriteMethodSources = new[]
        {
            typeof(NetBinaryWriter),
            typeof(NetBinaryWriterTypeExtensions),
            typeof(NetBinaryWriterNbtExtensions),
        };

        public NetPacketEncoder() : base()
        {
            RegisterDataTypes();
        }

        #region RegisterDataType[s]

        protected override void RegisterDataType(params Type[] arguments)
        {
            RegisterDataTypeFromMethod(_binaryWriterWriteMethodSources, "Write", arguments);
        }

        protected virtual void RegisterDataTypes()
        {
            // TODO: add attribute for auto-registering

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

            RegisterDataType(typeof(NetBinaryWriter), typeof(Chat));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Angle));
            RegisterDataType(typeof(NetBinaryWriter), typeof(Position));
            RegisterDataType(typeof(NetBinaryWriter), typeof(UUID));
        }

        #endregion

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsServerPacket);
        }

        public NetPacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (NetPacketWriterDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override Delegate CreateCoderDelegate(PacketStructInfo structInfo)
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

            var writerDelegate = typeof(NetPacketWriterDelegate<>).MakeGenericType(structInfo.Type);
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
            // TODO: respect LengthConstraint

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

                (ParameterExpression? Writer, Expression[] Args) writeInfo;
                MethodInfo? writeMethod;

                Type? writeType;
                if (isEnumProperty)
                {
                    // Enums get sligthly special treatment.
                    writeType = propertyInfo.Type.GetEnumUnderlyingType();
                    var enumTypeKey = DataTypeKey.FromVoid(writeType);
                    if (!DataTypeHandlers.TryGetValue(enumTypeKey, out writeMethod))
                        throw new Exception("Missing enum write method for \"" + enumTypeKey + "\".");

                    writeInfo = (writerParam, new[] { Expression.Convert(property, writeType) });
                }
                else
                {
                    writeType = propertyInfo.Type;

                    var writeInfos = new[]
                    {
                        (Writer: writerParam,
                        Args: new Expression[] { property },
                        Key: new DataTypeKey(typeof(void), writeType)),

                        (Writer: null,
                        Args: new Expression[] { writerParam, property },
                        Key: new DataTypeKey(typeof(void), typeof(NetBinaryWriter), writeType)),
                    };

                    writeInfo = default;
                    writeMethod = null;
                    foreach (var tWriteInfo in writeInfos)
                    {
                        if (DataTypeHandlers.TryGetValue(tWriteInfo.Key, out writeMethod))
                        {
                            writeInfo = (tWriteInfo.Writer, tWriteInfo.Args);
                            break;
                        }
                    }

                    if (writeMethod == null)
                        throw new Exception("Missing write method for \"" + writeType + "\".");
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
                    else if (lengthPrefixedAttrib.LengthSource == LengthSource.WrittenBytes)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Unknown length source: " + lengthPrefixedAttrib.LengthSource);
                    }
                }

                // TODO: write collections

                expressions.Add(Expression.Call(writeInfo.Writer, writeMethod, writeInfo.Args));
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
