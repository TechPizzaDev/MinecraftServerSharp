using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.DataTypes;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to transforms that turn packets into network messages.
    /// </summary>
    public partial class NetPacketEncoder : NetPacketCoder<ServerPacketID>
    {
        public delegate void PacketWriterDelegate<in TPacket>(TPacket packet, NetBinaryWriter writer);

        #region Constructors

        public NetPacketEncoder() : base()
        {
            RegisterDataTypesFromBinaryWriter();
        }

        private void RegisterDataTypesFromBinaryWriter()
        {
            void Register(string name, params Type[] args)
            {
                RegisterDataTypeFrom(typeof(NetBinaryWriter), name, args);
            }

            Register("Write", typeof(bool));
            Register("Write", typeof(sbyte));
            Register("Write", typeof(byte));
            Register("Write", typeof(short));
            Register("Write", typeof(ushort));
            Register("Write", typeof(int));
            Register("Write", typeof(long));

            Register("Write", typeof(VarInt));
            Register("Write", typeof(VarLong));

            Register("Write", typeof(float));
            Register("Write", typeof(double));

            Register("Write", typeof(Utf8String));
            Register("Write", typeof(string));
        }

        #endregion

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsServerPacket);
        }

        public PacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (PacketWriterDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override Delegate CreateCoderDelegate(PacketStructInfo structInfo)
        {
            var writerParam = Expression.Parameter(typeof(NetBinaryWriter), "Writer");
            var packetParam = Expression.Parameter(structInfo.Type, "Packet");

            Expression lambdaBody;
            if (typeof(IWritablePacket).IsAssignableFrom(structInfo.Type))
            {
                string methodName = nameof(IWritablePacket.Write);
                var writeMethod = structInfo.Type.GetMethod(methodName, new[] { writerParam.Type });
                var writePacketCall = Expression.Call(packetParam, writeMethod, writerParam);
                lambdaBody = writePacketCall;
            }
            else
            {
                lambdaBody = CreateComplexPacketWriter(writerParam, packetParam);
            }

            var packetWriterDelegate = typeof(PacketWriterDelegate<>).MakeGenericType(structInfo.Type);
            var resultLambda = Expression.Lambda(packetWriterDelegate, lambdaBody, new[] { packetParam, writerParam });
            var resultDelegate = resultLambda.Compile();
            return resultDelegate;
        }

        private BlockExpression CreateComplexPacketWriter(
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

            // cache property list for quick access
            var propertyList = packetProperties.Where(x => x != null).ToList();
            propertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            var expressionList = new Expression[propertyList.Count];
            for (int i = 0; i < propertyList.Count; i++)
            {
                var property = propertyList[i];
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
                
                expressionList[i] = call;
            }

            var expressionBlock = Expression.Block(expressionList);
            return expressionBlock;

        }
    }
}
