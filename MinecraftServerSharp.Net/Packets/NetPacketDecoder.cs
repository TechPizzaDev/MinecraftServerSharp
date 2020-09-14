using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Net.Packets
{
    /// <summary>
    /// Gives access to delegates that turn message data into packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder<ClientPacketId>
    {
        public delegate OperationStatus PacketReaderDelegate<TPacket>(NetBinaryReader reader, out TPacket packet);

        private static Type[] _binaryReaderTypes = new[] 
        {
            typeof(NetBinaryReader),
            typeof(NetBinaryReaderTypeExtensions)
        };

        public NetPacketDecoder() : base()
        {
            RegisterDataTypes();
        }

        #region RegisterDataType[s]

        protected override void RegisterDataType(params Type[] arguments)
        {
            RegisterDataTypeFromMethod(_binaryReaderTypes, "Read", arguments);
        }

        protected virtual void RegisterDataTypes()
        {
            void RegisterDataTypeAsOut(Type outType)
            {
                RegisterDataType(outType.MakeByRefType());
            }

            RegisterDataTypeAsOut(typeof(bool));
            RegisterDataTypeAsOut(typeof(sbyte));
            RegisterDataTypeAsOut(typeof(byte));
            RegisterDataTypeAsOut(typeof(short));
            RegisterDataTypeAsOut(typeof(ushort));
            RegisterDataTypeAsOut(typeof(int));
            RegisterDataTypeAsOut(typeof(long));

            RegisterDataTypeAsOut(typeof(VarInt));
            RegisterDataTypeAsOut(typeof(VarLong));

            RegisterDataTypeAsOut(typeof(float));
            RegisterDataTypeAsOut(typeof(double));

            RegisterDataTypeAsOut(typeof(Utf8String));
            RegisterDataTypeAsOut(typeof(string));

            RegisterDataTypeAsOut(typeof(Position));
        }

        #endregion

        public void RegisterClientPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsClientPacket);
        }

        public PacketReaderDelegate<TPacket> GetPacketReader<TPacket>()
        {
            return (PacketReaderDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override Delegate CreateCoderDelegate(PacketStructInfo structInfo)
        {
            var constructors = structInfo.Type.GetConstructors();
            var constructorInfoList = constructors
                .Where(c => c.GetCustomAttribute<PacketConstructorAttribute>() != null)
                .Select(c => new PacketConstructorInfo(c, c.GetCustomAttribute<PacketConstructorAttribute>()!))
                .ToList();

            if (constructorInfoList.Count > 1)
                throw new Exception("Only one packet constructor may be defined.");

            var variables = new List<ParameterExpression>();
            var expressions = new List<Expression>();
            var constructorArgs = new List<Expression>();

            // TODO: instead of only giving readMethod a NetBinaryReader, 
            // give it a state object with user-defined values/objects
            var readerParam = Expression.Parameter(typeof(NetBinaryReader), "Reader");
            var outPacketParam = Expression.Parameter(structInfo.Type.MakeByRefType(), "Packet");

            var resultCodeVar = Expression.Variable(typeof(OperationStatus), "OperationStatus");
            variables.Add(resultCodeVar);

            NewExpression newPacket;
            LabelTarget? returnTarget = null;
            ConstructorInfo? constructor = constructorInfoList.Count > 0
                ? constructorInfoList[0].Constructor : null;

            if (constructor != null)
            {
                var constructorParams = constructor.GetParameters();

                if (constructorParams.Length == 2 &&
                    constructorParams[0].ParameterType == typeof(NetBinaryReader) &&
                    constructorParams[1].ParameterType == typeof(OperationStatus).MakeByRefType())
                {
                    constructorArgs.Add(readerParam);
                    constructorArgs.Add(resultCodeVar);
                }
                else
                {
                    returnTarget = Expression.Label("Return");

                    CreateComplexPacketReader(
                        variables, constructorArgs, expressions,
                        readerParam, resultCodeVar,
                        returnTarget, constructorParams);
                }
                newPacket = Expression.New(constructor, constructorArgs);
            }
            else
            {
                newPacket = Expression.New(structInfo.Type);
                expressions.Add(Expression.Assign(resultCodeVar, Expression.Constant(OperationStatus.Done)));
            }
            expressions.Add(Expression.Assign(outPacketParam, newPacket));

            if (returnTarget != null)
                expressions.Add(Expression.Label(returnTarget));

            expressions.Add(resultCodeVar); // Return the read code by putting it as the last expression.

            var delegateType = typeof(PacketReaderDelegate<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(variables, expressions);
            var lambda = Expression.Lambda(delegateType, lambdaBody, new[] { readerParam, outPacketParam });
            return lambda.Compile();
        }

        private void CreateComplexPacketReader(
            List<ParameterExpression> variables,
            List<Expression> constructorArgs,
            List<Expression> expressions,
            ParameterExpression readerParam,
            ParameterExpression resultCodeVar,
            LabelTarget returnTarget,
            ParameterInfo[] constructorParams)
        {
            for (int i = 0; i < constructorParams.Length; i++)
            {
                var constructorParam = constructorParams[i];
                var resultVar = Expression.Variable(constructorParam.ParameterType, constructorParam.Name);
                if (constructorParam.ParameterType.IsByRef)
                    throw new Exception("An implicit packet constructor may not contain by-ref parameters.");

                variables.Add(resultVar);
                constructorArgs.Add(resultVar);

                var dataTypeKey = new DataTypeKey(
                    typeof(OperationStatus), constructorParam.ParameterType.MakeByRefType());

                var readMethod = DataTypeHandlers[dataTypeKey];
                var readCall = Expression.Call(readerParam, readMethod, arguments: resultVar);
                expressions.Add(Expression.Assign(resultCodeVar, readCall));

                expressions.Add(Expression.IfThen(
                    test: Expression.NotEqual(resultCodeVar, Expression.Constant(OperationStatus.Done)),
                    ifTrue: Expression.Goto(returnTarget)));
            }
        }
    }
}
