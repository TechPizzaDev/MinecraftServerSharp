using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to transforms that turn network messages into packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder
    {
        public delegate TPacket PacketReaderDelegate<out TPacket>(NetBinaryReader reader);

        #region Constructors

        public NetPacketDecoder() : base()
        {
            RegisterDataTypesFromBinaryReader();
        }

        private void RegisterDataTypesFromBinaryReader()
        {
            void Register(string method, Type[] types = null)
            {
                RegisterTypeReaderFromBinary(typeof(NetBinaryReader), method, types);
            }

            Register(nameof(NetBinaryReader.ReadBoolean));
            Register(nameof(NetBinaryReader.ReadSByte));
            Register(nameof(NetBinaryReader.ReadByte));
            Register(nameof(NetBinaryReader.ReadInt16));
            Register(nameof(NetBinaryReader.ReadUInt16));
            Register(nameof(NetBinaryReader.ReadInt32));
            Register(nameof(NetBinaryReader.ReadInt64));

            Register(nameof(NetBinaryReader.ReadVarInt32));
            Register(nameof(NetBinaryReader.ReadVarInt64));

            Register(nameof(NetBinaryReader.ReadSingle));
            Register(nameof(NetBinaryReader.ReadDouble));

            Register(nameof(NetBinaryReader.ReadUtf8String));
            Register(nameof(NetBinaryReader.ReadUtf8String), new[] { typeof(int) });

            Register(nameof(NetBinaryReader.ReadString));
            Register(nameof(NetBinaryReader.ReadString), new[] { typeof(int) });
        }

        #endregion

        public void RegisterClientPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsClientPacket);
        }

        public PacketReaderDelegate<TPacket> GetPacketReader<TPacket>()
        {
            return (PacketReaderDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override void PreparePacketType(PacketStructInfo packetInfo)
        {
            if (PreparedPacketCoders.ContainsKey(packetInfo.Type))
                return;

            var constructors = packetInfo.Type.GetConstructors();
            var constructorInfoList = constructors
                .Select(c => new PacketConstructorInfo(c, c.GetCustomAttribute<PacketConstructorAttribute>()))
                .Where(x => x.Attribute != null)
                .ToList();

            if (constructorInfoList.Count == 0)
                throw new Exception("No packet constructors are defined.");

            if (constructorInfoList.Count > 1)
                throw new Exception("Only one packet constructor may be specified.");

            var constructorInfo = constructorInfoList[0];
            var constructorParameters = constructorInfo.Constructor.GetParameters();
            var lengthFromInfoList = GetLengthFromAttributeInfos(constructorParameters).ToList();

            // TODO: instead of only giving the readMethod a NetBinaryReader, 
            // give it a state object with user-defined values/objects
            var readerParam = Expression.Parameter(typeof(NetBinaryReader), "Reader");

            var variableList = new List<ParameterExpression>();
            var expressionList = new List<Expression>();
            for (int i = 0; i < constructorParameters.Length; i++)
            {
                var parameter = constructorParameters[i];
                var resultVariable = Expression.Variable(parameter.ParameterType, parameter.Name);

                Expression CreateReadExpression(MethodInfo readMethod, params Expression[] arguments)
                {
                    var invocation = Expression.Call(readerParam, readMethod, arguments);
                    var assignment = Expression.Assign(resultVariable, invocation);
                    return assignment;
                }

                Expression resultExpression;
                var lengthFromAttrib = lengthFromInfoList.FirstOrDefault(x => x.Target == parameter);
                if (lengthFromAttrib.HasValue)
                {
                    int sourceParamIndex = Array.IndexOf(constructorParameters, lengthFromAttrib.Source);
                    var lengthSourceExpression = variableList[sourceParamIndex];
                    var lengthReadIntResult = Expression.Convert(lengthSourceExpression, typeof(int));

                    var readMethod = LengthPrefixedDataTypes[parameter.ParameterType];
                    resultExpression = CreateReadExpression(readMethod, lengthReadIntResult);
                }
                else
                {
                    var readMethod = DataTypes[parameter.ParameterType];
                    resultExpression = CreateReadExpression(readMethod);
                }

                variableList.Add(resultVariable);
                expressionList.Add(resultExpression);
            }

            var packetConstruct = Expression.New(constructorInfo.Constructor, variableList);
            expressionList.Add(packetConstruct);

            var packetReaderDelegate = typeof(PacketReaderDelegate<>).MakeGenericType(packetInfo.Type);
            var expressionBlock = Expression.Block(variableList, expressionList);
            var resultDelegate = Expression.Lambda(packetReaderDelegate, expressionBlock, readerParam).Compile();
            PreparedPacketCoders.Add(packetInfo.Type, resultDelegate);
        }
    }
}
