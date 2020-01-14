using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    public enum ResultCode
    {
        Ok,
        EndOfStream
    }

    /// <summary>
    /// Gives access to transforms that turn network messages into packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder<ClientPacketID>
    {
        public delegate ResultCode PacketReaderDelegate<TPacket>(NetBinaryReader reader, out TPacket packet);

        #region Constructors

        public NetPacketDecoder() : base()
        {
            RegisterDataTypesFromBinaryReader();
        }

        private void RegisterDataTypesFromBinaryReader()
        {
            void Register(string method) => RegisterDataTypeFrom(typeof(NetBinaryReader), method);

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

        protected override Delegate CreateCoderDelegate(PacketStructInfo structInfo)
        {
            var constructors = structInfo.Type.GetConstructors();
            var constructorInfoList = constructors
                .Select(c => new PacketConstructorInfo(c, c.GetCustomAttribute<PacketConstructorAttribute>()))
                .Where(x => x.Attribute != null)
                .ToList();

            if (constructorInfoList.Count == 0)
                throw new Exception("No packet constructors are defined.");

            if (constructorInfoList.Count > 1)
                throw new Exception("Only one packet constructor may be specified.");

            var constructInfo = constructorInfoList[0];
            var constructParams = constructInfo.Constructor.GetParameters();
            
            var variables = new List<ParameterExpression>();
            var expressions = new List<Expression>();
            var constructorArgs = new List<Expression>();

            // TODO: instead of only giving readMethod a NetBinaryReader, 
            // give it a state object with user-defined values/objects
            var readerParam = Expression.Parameter(typeof(NetBinaryReader), "Reader");
            var resultCodeParam = Expression.Parameter(typeof(ResultCode), "ResultCode");

            if (constructParams.Length == 2 &&
                constructParams[0].ParameterType == typeof(NetBinaryReader) &&
                constructParams[1].ParameterType == typeof(ResultCode))
            {
                if (!constructParams[1].IsOut)
                    throw new Exception();

                constructorArgs.Add(readerParam);
                constructorArgs.Add(resultCodeParam);
            }
            else
            {
                CreateComplexPacketReader(
                    variables, constructorArgs, expressions,
                    readerParam, resultCodeParam, 
                    constructParams, constructInfo);
            }

            var construct = Expression.New(constructInfo.Constructor, constructorArgs);
            expressions.Add(construct);

            expressions.Add(resultCodeParam); // return the result code
            
            var packetReaderDelegate = typeof(PacketReaderDelegate<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(variables, expressions);
            var resultLambda = Expression.Lambda(packetReaderDelegate, lambdaBody, new[] { readerParam });
            var resultDelegate = resultLambda.Compile();
            return resultDelegate;
        }

        private BlockExpression CreateComplexPacketReader(
            List<ParameterExpression> variables,
            List<Expression> constructorArgs,
            List<Expression> expressions,
            ParameterExpression readerParam,
            ParameterExpression successParam,
            ParameterInfo[] parameters,
            PacketConstructorInfo constructorInfo)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var result = Expression.Variable(parameter.ParameterType, parameter.Name);

                var readMethod = DataTypes[new DataTypeKey(parameter.ParameterType)];
                var call = Expression.Call(readerParam, readMethod);
                var assign = Expression.Assign(result, call);

                variables.Add(constructorArgs);
                constructorArgs.Add(result);
                expressions.Add(assign);
            }
        }
    }
}
