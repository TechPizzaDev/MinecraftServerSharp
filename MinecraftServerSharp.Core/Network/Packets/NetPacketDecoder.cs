using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MinecraftServerSharp.DataTypes;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Transforms network messages into comprehensible packets.
    /// </summary>
    public partial class NetPacketDecoder : NetPacketCoder
    {
        public delegate T TypeReaderDelegate<out T>(INetBinaryReader reader);
        public delegate T SpecialTypeReaderDelegate<out T>(INetBinaryReader reader, ExtendedPropertyInfo propertyInfo);

        //private readonly struct ReadDelegate
        //{
        //    public Delegate Delegate { get; }
        //    public bool IsSpecial { get; }
        //
        //    public ReadDelegate(Delegate @delegate)
        //    {
        //        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        //        var genericTypeDef = @delegate.GetType().GetGenericTypeDefinition();
        //        if (genericTypeDef != typeof(TypeReaderDelegate<>) &&
        //            genericTypeDef != typeof(SpecialTypeReaderDelegate<>))
        //            throw new ArgumentException(string.Format(
        //                "The delegate is not of the type \"{0}\" or \"{1}\"",
        //                typeof(TypeReaderDelegate<>),
        //                typeof(SpecialTypeReaderDelegate<>)));
        //
        //        IsSpecial = genericTypeDef == typeof(SpecialTypeReaderDelegate<>);
        //    }
        //}

        private static ParameterExpression ReaderParameter { get; }
        private static Dictionary<Type, Delegate> TypeReaders { get; }
        private static Dictionary<Type, InvocationExpression> SpecialTypeReaders { get; }

        static NetPacketDecoder()
        {
            ReaderParameter = Expression.Parameter(typeof(INetBinaryReader));
            TypeReaders = new Dictionary<Type, Delegate>();
            SpecialTypeReaders = new Dictionary<Type, InvocationExpression>();

            RegisterTypeReadersFromBinaryReaders();
        }

        #region RegisterTypeReadersFromBinaryReaders

        private static void RegisterTypeReadersFromBinaryReaders()
        {
            RegisterTypeReaderFromBinaryReader<bool>(nameof(INetBinaryReader.ReadBoolean));
            RegisterTypeReaderFromBinaryReader<sbyte>(nameof(INetBinaryReader.ReadSByte));
            RegisterTypeReaderFromBinaryReader<byte>(nameof(INetBinaryReader.ReadByte));

            RegisterTypeReaderFromBinaryReader<char>(nameof(INetBinaryReader.ReadChar));
            RegisterTypeReaderFromBinaryReader<short>(nameof(INetBinaryReader.ReadInt16));
            RegisterTypeReaderFromBinaryReader<ushort>(nameof(INetBinaryReader.ReadUInt16));

            RegisterTypeReaderFromBinaryReader<int>(nameof(INetBinaryReader.ReadInt32));
            RegisterTypeReaderFromBinaryReader<uint>(nameof(INetBinaryReader.ReadUInt32));
            RegisterTypeReaderFromBinaryReader<VarInt32>(nameof(INetBinaryReader.ReadVarInt32));

            RegisterTypeReaderFromBinaryReader<long>(nameof(INetBinaryReader.ReadInt64));
            RegisterTypeReaderFromBinaryReader<ulong>(nameof(INetBinaryReader.ReadUInt64));
            RegisterTypeReaderFromBinaryReader<VarInt64>(nameof(INetBinaryReader.ReadVarInt64));

            RegisterTypeReaderFromBinaryReader<float>(nameof(INetBinaryReader.ReadSingle));
            RegisterTypeReaderFromBinaryReader<double>(nameof(INetBinaryReader.ReadDouble));
            RegisterTypeReaderFromBinaryReader<decimal>(nameof(INetBinaryReader.ReadDecimal));

            RegisterTypeReaderFromBinaryReader<string>(nameof(INetBinaryReader.ReadString));
            RegisterTypeReaderFromBinaryReader<Utf8String>(nameof(INetBinaryReader.ReadUtf8String));
        }

        private static void RegisterTypeReaderFromBinaryReader<TReturn>(string name)
        {
            try
            {
                var method = typeof(INetBinaryReader).GetMethod(name, Array.Empty<Type>());
                var simpleDelegate = method.CreateDelegate<TypeReaderDelegate<TReturn>>();
                RegisterTypeReader(typeof(TReturn), simpleDelegate);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create type reader from binary reader method \"{name}\".", ex);
            }
        }

        #endregion

        //private static void RegisterTypeReadersForStrings()
        //{
        //    SpecialTypeReaderDelegate<string> string16ReaderDelegate = (reader, currentProperty) =>
        //    {
        //        int length;
        //        if (currentProperty.LengthAttributeInfo != null)
        //        {
        //            throw new NotImplementedException();
        //        }
        //        else
        //        {
        //            length = reader.ReadVarInt32();
        //            return reader.ReadString(length);
        //        }
        //    };
        //    SpecialTypeReaders.Add(typeof(string), string16ReaderDelegate);
        //
        //    SpecialTypeReaderDelegate<Utf8String> string8ReaderDelegate = (reader) =>
        //    {
        //        return reader.ReadUtf8String(length);
        //        int length;
        //        if (currentProperty.LengthAttributeInfo != null)
        //        {
        //            throw new NotImplementedException();
        //        }
        //        else
        //        {
        //            length = reader.ReadVarInt32();
        //        }
        //    };
        //    RegisterTypeReader(typeof(Utf8String), string8ReaderDelegate);
        //}

        public static void RegisterTypeReader<T>(Type type, TypeReaderDelegate<T> readDelegate)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (readDelegate == null) throw new ArgumentNullException(nameof(readDelegate));

            lock (TypeReaders)
                TypeReaders.Add(type, readDelegate);
        }

        public void RegisterClientPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsClientPacket);
        }

        protected override void PreparePacketTypeCore(
            PacketStructInfo packetInfo,
            List<PacketPropertyInfo> propertyList,
            List<PacketPropertyLengthAttributeInfo> lengthAttributeList)
        {
            var constructors = packetInfo.Type.GetConstructors();
            var packetConstructorList = constructors
                .Select(c => new PacketConstructorInfo(c, c.GetCustomAttribute<PacketConstructorAttribute>()))
                .Where(x => x.Attribute != null)
                .ToList();

            if (packetConstructorList.Count == 0)
                throw new Exception("No packet constructors are defined.");

            if (packetConstructorList.Count > 1)
                throw new Exception("Only one packet constructor may be specified.");

            var packetConstructor = packetConstructorList[0];
            var constructorParameters = packetConstructor.Constructor.GetParameters();

            lock (TypeReaders)
            {
                foreach (var constructorParameter in constructorParameters)
                {
                    if (!TypeReaders.ContainsKey(constructorParameter.ParameterType))
                        throw new Exception($"No type reader for \"{constructorParameter.ParameterType}\".");
                }
            }

            var propertyParamPairList = propertyList.Join(
                constructorParameters,
                property => property.Name,
                parameter => parameter.Name,
                (property, parameter) => (property, parameter),
                StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lengthAttribPairList = propertyList.Join(
                lengthAttributeList,
                propertyInfo => propertyInfo.Name,
                lengthAttribInfo => lengthAttribInfo.SourceProperty.Name,
                (propertyInfo, lengthAttribInfo) => (propertyInfo, lengthAttribInfo),
                StringComparer.Ordinal)
                .ToList();

            if (propertyParamPairList.Count != propertyList.Count)
                throw new Exception("Failed to map all properties to constructor parameters.");

            foreach (var (property, parameter) in propertyParamPairList)
            {
                if (property.LengthAttribute != null)
                {
                    var (sourceProperty, lengthAttribInfo) = lengthAttribPairList
                        .First(x => x.lengthAttribInfo.TargetProperty == property);
                    
                    var lengthReadDelegate = TypeReaders[sourceProperty.Type];
                    var readDelegate = TypeReaders[parameter.ParameterType];
                }
                else
                {
                    var readDelegate = TypeReaders[parameter.ParameterType];
                }
            }

            //var paramExpressionList = new Stack<ParameterExpression>();
            //foreach(var (property, parameter) in propertyParamPairList)
            //{
            //    var paramExpression = Expression.Parameter(parameter.ParameterType);
            //    paramExpressionStack.Push(paramExpression);
            //
            //    Console.WriteLine(property.Name);
            //    Console.WriteLine(parameter.Name);
            //}

        }
    }
}
