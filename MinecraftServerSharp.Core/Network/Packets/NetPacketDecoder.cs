using System;
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
            void Register(string method) => RegisterTypeReaderFromBinary(typeof(NetBinaryReader), method);

            Register(nameof(NetBinaryReader.ReadBool));
            Register(nameof(NetBinaryReader.ReadSByte));
            Register(nameof(NetBinaryReader.ReadByte));
            Register(nameof(NetBinaryReader.ReadShort));
            Register(nameof(NetBinaryReader.ReadUShort));
            Register(nameof(NetBinaryReader.ReadInt));
            Register(nameof(NetBinaryReader.ReadLong));

            Register(nameof(NetBinaryReader.ReadVarInt));
            Register(nameof(NetBinaryReader.ReadVarLong));

            Register(nameof(NetBinaryReader.ReadFloat));
            Register(nameof(NetBinaryReader.ReadDouble));

            Register(nameof(NetBinaryReader.ReadString));
            Register(nameof(NetBinaryReader.ReadUtf16String));
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

        protected override void PreparePacketType(PacketStructInfo structInfo)
        {
            if (PreparedPacketCoders.ContainsKey(structInfo.Type))
                return;

            var constructors = structInfo.Type.GetConstructors();
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

            // TODO: instead of only giving readMethod a NetBinaryReader, 
            // give it a state object with user-defined values/objects
            var readerParam = Expression.Parameter(typeof(NetBinaryReader), "Reader");

            Expression lambdaBody;
            if (constructorParameters.Length == 1 &&
                constructorParameters[0].ParameterType == typeof(NetBinaryReader))
            {
                var argumentList = new[] { readerParam };
                var packetConstruct = Expression.New(constructorInfo.Constructor, argumentList);
                lambdaBody = packetConstruct;
            }
            else
            {
                lambdaBody = CreateComplexPacketReader(
                    constructorParameters, readerParam, constructorInfo);
            }

            var packetReaderDelegate = typeof(PacketReaderDelegate<>).MakeGenericType(structInfo.Type);
            var resultLambda = Expression.Lambda(packetReaderDelegate, lambdaBody, readerParam);
            var resultDelegate = resultLambda.Compile();
            PreparedPacketCoders.Add(structInfo.Type, resultDelegate);
        }

        private BlockExpression CreateComplexPacketReader(
            ParameterInfo[] parameters,
            ParameterExpression readerParam,
            PacketConstructorInfo constructorInfo)
        {
            var variableList = new ParameterExpression[parameters.Length];
            var expressionList = new Expression[parameters.Length + 1];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var resultVariable = Expression.Variable(parameter.ParameterType, parameter.Name);

                var readMethod = DataTypes[parameter.ParameterType];
                var invocation = Expression.Call(readerParam, readMethod);
                var resultExpression = Expression.Assign(resultVariable, invocation);

                variableList[i] = resultVariable;
                expressionList[i] = resultExpression;
            }

            var packetConstruct = Expression.New(constructorInfo.Constructor, variableList);
            expressionList[parameters.Length] = packetConstruct;

            var expressionBlock = Expression.Block(variableList, expressionList);
            return expressionBlock;
        }
    }
}
