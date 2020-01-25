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
    public partial class NetPacketDecoder : NetPacketCoder<ClientPacketID>
    {
        public delegate ReadCode PacketReaderDelegate<TPacket>(NetBinaryReader reader, out TPacket packet);

        #region Constructors

        public NetPacketDecoder() : base()
        {
            RegisterDataTypesFromBinaryReader();
        }

        private void RegisterDataTypesFromBinaryReader()
        {
            void Register(string method) => RegisterDataTypeFrom(typeof(NetBinaryReader), method);

            Register(nameof(NetBinaryReader.Read));
            Register(nameof(NetBinaryReader.ReadSByte));
            Register(nameof(NetBinaryReader.Read));
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
            var outPacketParam = Expression.Parameter(structInfo.Type.MakeByRefType(), "Packet");

            var resultCodeVariable = Expression.Variable(typeof(ReadCode), "ResultCode");
            variables.Add(resultCodeVariable);
            
            expressions.Add(Expression.Assign(
                resultCodeVariable, Expression.Default(resultCodeVariable.Type)));

            bool isComplex;

            if (constructParams.Length == 2 &&
                constructParams[0].ParameterType == typeof(NetBinaryReader) &&
                constructParams[1].ParameterType == typeof(ReadCode).MakeByRefType())
            {
                constructorArgs.Add(readerParam);
                constructorArgs.Add(resultCodeVariable);
                isComplex = false;
            }
            else
            {
                CreateComplexPacketReader(
                    variables, constructorArgs, expressions,
                    readerParam, resultCodeVariable, constructParams);
                isComplex = true;
            }

            var newPacket = Expression.New(constructInfo.Constructor, constructorArgs);
            var assignPacket = Expression.Assign(outPacketParam, newPacket);

            if (isComplex)
            {
                var codeOkCheck = Expression.Equal(resultCodeVariable, Expression.Constant(ReadCode.Ok));
                var conditional = Expression.IfThen(codeOkCheck, assignPacket);
                expressions.Add(conditional);
            }
            else
            {
                expressions.Add(assignPacket);
            }
            expressions.Add(resultCodeVariable); // return the code by adding it at the end of the block
            
            var packetReaderDelegate = typeof(PacketReaderDelegate<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(variables, expressions);
            var lambdaParams = new[] { readerParam, outPacketParam };
            var resultLambda = Expression.Lambda(packetReaderDelegate, lambdaBody, lambdaParams);
            var resultDelegate = resultLambda.Compile();
            return resultDelegate;
        }

        private void CreateComplexPacketReader(
            List<ParameterExpression> variables,
            List<Expression> constructorArgs,
            List<Expression> expressions,
            ParameterExpression readerParam,
            ParameterExpression resultCodeParam,
            ParameterInfo[] constructorParams)
        {
            for (int i = 0; i < constructorParams.Length; i++)
            {
                var constructorParam = constructorParams[i];
                var result = Expression.Variable(constructorParam.ParameterType, constructorParam.Name);

                var readMethod = ReadMethods[new DataTypeKey(constructorParam.ParameterType)];
                var call = Expression.Call(readerParam, readMethod);
                var assign = Expression.Assign(result, call);

                variables.Add(result);
                constructorArgs.Add(result);
                expressions.Add(assign);
            }

            var okAssign = Expression.Assign(resultCodeParam, Expression.Constant(ReadCode.Ok));
            expressions.Add(okAssign);
        }
    }
}
