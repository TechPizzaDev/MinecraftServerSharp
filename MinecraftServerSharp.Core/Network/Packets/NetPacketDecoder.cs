using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Transforms network messages into comprehensible packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder
    {
        public delegate TPacket PacketReaderDelegate<out TPacket>(NetBinaryReader reader);

        private static ParameterExpression ReaderParameter { get; }
        private static Dictionary<Type, MethodInfo> TypeReaders { get; }
        private static Dictionary<Type, MethodInfo> LengthTypeReaders { get; }
        private static Dictionary<Type, Delegate> PacketReaders { get; }

        static NetPacketDecoder()
        {
            ReaderParameter = Expression.Parameter(typeof(NetBinaryReader));
            TypeReaders = new Dictionary<Type, MethodInfo>();
            LengthTypeReaders = new Dictionary<Type, MethodInfo>();
            PacketReaders = new Dictionary<Type, Delegate>();

            RegisterTypeReadersFromBinaryReaders();
        }

        #region RegisterTypeReadersFromBinaryReaders

        private static void RegisterTypeReadersFromBinaryReaders()
        {
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadBoolean));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadSByte));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadByte));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadInt16));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadUInt16));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadInt32));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadInt64));

            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadVarInt32));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadVarInt64));

            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadSingle));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadDouble));

            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadUtf8String));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadUtf8String), new[] { typeof(int) });

            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadString));
            RegisterTypeReaderFromBinaryReader(nameof(NetBinaryReader.ReadString), new[] { typeof(int) });
        }

        private static void RegisterTypeReaderFromBinaryReader(string name, Type[] types = null)
        {
            try
            {
                var method = typeof(NetBinaryReader).GetMethod(name, types ?? Array.Empty<Type>());
                if (types?.Length == 1 && types[0] == typeof(int))
                {
                    RegisterLengthTypeReader(method);
                }
                else
                {
                    RegisterTypeReader(method);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create type reader from binary reader method \"{name}\".", ex);
            }
        }

        #endregion

        [DebuggerHidden]
        private static void ValidateTypeReaderArgs(
            Type type, MethodInfo method, params Type[] paramTypes)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (method == null) throw new ArgumentNullException(nameof(method));

            // currently never throws
            //if (method.ReturnType != type)
            //    throw new ArgumentException("Method does not return the specified type.");

            if (!method.GetParameters().Select(x => x.ParameterType).SequenceEqual(paramTypes))
                throw new ArgumentException("Method contains undesired parameters.");
        }

        public static void RegisterTypeReader(MethodInfo method)
        {
            ValidateTypeReaderArgs(method.ReturnType, method, Array.Empty<Type>());

            lock (TypeReaders)
                TypeReaders.Add(method.ReturnType, method);
        }

        public static void RegisterLengthTypeReader(MethodInfo method)
        {
            ValidateTypeReaderArgs(method.ReturnType, method, new[] { typeof(int) });

            lock (LengthTypeReaders)
                LengthTypeReaders.Add(method.ReturnType, method);
        }

        public void RegisterClientPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsClientPacket);
        }

        public Delegate GetPacketReader(Type packetType)
        {
            if (!PacketReaders.TryGetValue(packetType, out var reader))
            {
                PreparePacketType(new PacketStructInfo(packetType));
                reader = PacketReaders[packetType];
            }
            return reader;
        }

        public PacketReaderDelegate<TPacket> GetPacketReader<TPacket>()
        {
            return (PacketReaderDelegate<TPacket>)GetPacketReader(typeof(TPacket));
        }

        protected override void PreparePacketType(PacketStructInfo packetInfo)
        {
            if (PacketReaders.ContainsKey(packetInfo.Type))
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

                    var readMethod = LengthTypeReaders[parameter.ParameterType];
                    resultExpression = CreateReadExpression(readMethod, lengthReadIntResult);
                }
                else
                {
                    var readMethod = TypeReaders[parameter.ParameterType];
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
            PacketReaders.Add(packetInfo.Type, resultDelegate);
        }
    }
}
