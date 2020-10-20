using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MCServerSharp.Data.IO;
using MCServerSharp.NBT;

namespace MCServerSharp.Net.Packets
{
    public delegate OperationStatus NetPacketReaderAction<TPacket>(NetBinaryReader reader, out TPacket packet);

    /// <summary>
    /// Gives access to delegates that turn message data into packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder<ClientPacketId>
    {
        private static Type[] _binaryReaderTypes = new[]
        {
            typeof(NetBinaryReader),
            typeof(NetBinaryReaderTypeExtensions),
            typeof(NetBinaryReaderNbtExtensions)
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
            void RegisterDataTypeAsOut(params Type[] outType)
            {
                RegisterDataType(outType.SkipLast(1).Append(outType.Last().MakeByRefType()).ToArray());
            }

            // TODO: add attribute for auto-registering

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

            RegisterDataTypeAsOut(typeof(NetBinaryReader), typeof(Identifier));
            RegisterDataTypeAsOut(typeof(NetBinaryReader), typeof(Position));
            RegisterDataTypeAsOut(typeof(NetBinaryReader), typeof(Slot));
            RegisterDataTypeAsOut(typeof(NetBinaryReader), typeof(NbtDocument));
        }

        #endregion

        public void RegisterClientPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.Attribute.IsClientPacket);
        }

        public NetPacketReaderAction<TPacket> GetPacketReaderAction<TPacket>()
        {
            return (NetPacketReaderAction<TPacket>)GetPacketAction(typeof(TPacket));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// <para>
        /// A manual read pattern is inferred by using
        /// <c>(<see cref="NetBinaryReader"/>, <see langword="out"/> <see cref="OperationStatus"/>)</c>
        /// parameters.
        /// </para>
        /// <para>
        /// The packet constructor is not called if data is malformed.
        /// Empty packet constructors are called without reading packet data 
        /// (always returning <see cref="OperationStatus.Done"/>).
        /// </para>
        /// </remarks>
        public override Delegate CreatePacketAction(PacketStructInfo structInfo)
        {
            var constructors = structInfo.Type.GetConstructors();
            var constructorInfoList = constructors
                .Where(c => c.GetCustomAttribute<PacketConstructorAttribute>() != null)
                .Select(c => new PacketConstructorInfo(c, c.GetCustomAttribute<PacketConstructorAttribute>()!))
                .ToList();

            if (constructorInfoList.Count > 1)
            {
                // TODO: Change this after PacketSwitch attribute for params is implemented
                throw new Exception("Only one packet constructor may be defined.");
            }

            var variables = new List<ParameterExpression>();
            var expressions = new List<Expression>();
            var constructorArgs = new List<Expression>();

            // TODO: instead of only giving readMethod a NetBinaryReader, 
            // give it a state object with user-defined values/objects
            var readerParam = Expression.Parameter(typeof(NetBinaryReader), "Reader");
            var outPacketParam = Expression.Parameter(structInfo.Type.MakeByRefType(), "Packet");

            var statusVar = Expression.Variable(typeof(OperationStatus), "Status");
            variables.Add(statusVar);

            // The return target allows code to jump out in the middle of packet reading.
            LabelTarget? returnTarget = null;

            ConstructorInfo? constructor = constructorInfoList.FirstOrDefault()?.Constructor;

            NewExpression newPacket;
            if (constructor == null)
            {
                // Struct with empty constructor doesn't require much logic
                newPacket = Expression.New(structInfo.Type);

                // Empty constructors really shouldn't ever fail...
                expressions.Add(Expression.Assign(statusVar, Expression.Constant(OperationStatus.Done)));
            }
            else
            {
                var constructorParams = constructor.GetParameters();

                // Look if the constructors want to use the read-pattern.
                if (constructorParams.Length == 2 &&
                    constructorParams[0].ParameterType == typeof(NetBinaryReader) &&
                    constructorParams[1].ParameterType == typeof(OperationStatus).MakeByRefType() &&
                    constructorParams[1].ParameterType.GetElementType() == typeof(OperationStatus))
                {
                    if (!constructorParams[1].Attributes.HasFlag(ParameterAttributes.Out))
                    {
                        throw new Exception(
                            $"The constructor parameter types match the read-pattern," +
                            $"but the {nameof(OperationStatus)} parameter is not an out parameter.");
                    }

                    constructorArgs.Add(readerParam);
                    constructorArgs.Add(statusVar);
                }
                else
                {
                    // Otherwise just create a read-sequence that calls 
                    // the constructor with it's params.
                    returnTarget = Expression.Label("Return");

                    CreatePacketReadSequence(
                        variables, constructorArgs, expressions,
                        readerParam, statusVar,
                        returnTarget, constructorParams);
                }

                // The constructor is not called if data is malformed.
                newPacket = Expression.New(constructor, constructorArgs);
            }
            expressions.Add(Expression.Assign(outPacketParam, newPacket));

            if (returnTarget != null)
                expressions.Add(Expression.Label(returnTarget));

            expressions.Add(statusVar); // Return the status by putting it as the last expression.

            var delegateType = typeof(NetPacketReaderAction<>).MakeGenericType(structInfo.Type);
            var lambdaBody = Expression.Block(variables, expressions);
            var lambda = Expression.Lambda(delegateType, lambdaBody, new[] { readerParam, outPacketParam });
            return lambda.Compile();
        }

        private void CreatePacketReadSequence(
            List<ParameterExpression> variables,
            List<Expression> constructorArgs,
            List<Expression> expressions,
            ParameterExpression readerParam,
            ParameterExpression statusVar,
            LabelTarget returnTarget,
            ParameterInfo[] constructorParams)
        {
            for (int i = 0; i < constructorParams.Length; i++)
            {
                var constructorParam = constructorParams[i];
                var paramType = constructorParam.ParameterType;
                if (paramType.IsByRef)
                    throw new Exception("A complex packet constructor may not contain by-ref parameters.");

                var resultVar = Expression.Variable(constructorParam.ParameterType, constructorParam.Name);
                variables.Add(resultVar);
                constructorArgs.Add(resultVar);

                var paramOutType = paramType.MakeByRefType();
                var methodTuples = new[]
                {
                    // Used for NetBinaryReader methods.
                    (Reader: readerParam,
                    Args: new[] { resultVar },
                    Key: new DataTypeKey(typeof(OperationStatus), paramOutType)),

                    // Used for extension methods.
                    (Reader: null,
                    Args: new[] { readerParam, resultVar },
                    Key: new DataTypeKey(typeof(OperationStatus), typeof(NetBinaryReader), paramOutType)),

                    // TODO: add a state object with a service collection that's passed to the method?
                };

                MethodInfo? dataReadMethod = null;
                foreach (var (reader, args, dataKey) in methodTuples)
                {
                    if (DataTypeHandlers.TryGetValue(dataKey, out dataReadMethod))
                    {
                        var readCall = Expression.Call(reader, dataReadMethod, args);
                        var statusAssign = Expression.Assign(statusVar, readCall);
                        expressions.Add(statusAssign);

                        var condition = Expression.IfThen(
                            test: Expression.NotEqual(statusVar, Expression.Constant(OperationStatus.Done)),
                            ifTrue: Expression.Goto(returnTarget));

                        expressions.Add(condition);
                        break;
                    }
                }
                if (dataReadMethod == null)
                    throw new Exception($"Failed to find data read method for {paramType}.");
            }
        }
    }
}
