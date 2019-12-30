using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder
    {
        protected Dictionary<Type, PacketStructInfo> RegisteredTypes { get; }
        protected Dictionary<Type, MethodInfo> DataTypes { get; }
        protected Dictionary<Type, MethodInfo> LengthPrefixedDataTypes { get; }
        protected Dictionary<Type, Delegate> PreparedPacketCoders { get; }

        public int RegisteredTypeCount => RegisteredTypes.Count;
        public int PreparedTypeCount => PreparedPacketCoders.Count;

        #region Constructors

        public NetPacketCoder()
        {
            RegisteredTypes = new Dictionary<Type, PacketStructInfo>();
            DataTypes = new Dictionary<Type, MethodInfo>();
            LengthPrefixedDataTypes = new Dictionary<Type, MethodInfo>();
            PreparedPacketCoders = new Dictionary<Type, Delegate>();
        }

        #endregion

        public Delegate GetPacketCoder(Type packetType)
        {
            if (!PreparedPacketCoders.TryGetValue(packetType, out var reader))
            {
                PreparePacketType(new PacketStructInfo(packetType));
                reader = PreparedPacketCoders[packetType];
            }
            return reader;
        }

        protected void RegisterTypeReaderFromBinary(Type binaryType, string methodName, Type[] types = null)
        {
            try
            {
                var method = binaryType.GetMethod(methodName, types ?? Array.Empty<Type>());
                if (types?.Length == 1 && types[0] == typeof(int))
                    RegisterLengthPrefixedDataType(method);
                else
                    RegisterDataType(method);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create data type from {binaryType}.{methodName}().", ex);
            }
        }

        [DebuggerHidden]
        private static void ValidateDataTypeArgs(
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

        public void RegisterDataType(MethodInfo method)
        {
            ValidateDataTypeArgs(method.ReturnType, method, Array.Empty<Type>());

            lock (DataTypes)
                DataTypes.Add(method.ReturnType, method);
        }

        public void RegisterLengthPrefixedDataType(MethodInfo method)
        {
            ValidateDataTypeArgs(method.ReturnType, method, new[] { typeof(int) });

            lock (LengthPrefixedDataTypes)
                LengthPrefixedDataTypes.Add(method.ReturnType, method);
        }

        public void RegisterPacketTypesFromCallingAssembly(Func<PacketStructInfo, bool> predicate)
        {
            var assembly = Assembly.GetCallingAssembly();
            var packetTypes = PacketStructInfo.GetPacketTypes(assembly);
            RegisterPacketTypes(packetTypes.Where(predicate));
        }

        public void RegisterPacketTypes(IEnumerable<PacketStructInfo> infos)
        {
            foreach (var info in infos)
                RegisterPacketType(info);
        }

        public void RegisterPacketType(PacketStructInfo info)
        {
            RegisteredTypes.Add(info.Type, info);
        }

        public void PreparePacketTypes()
        {
            foreach (var registeredType in RegisteredTypes)
                PreparePacketType(registeredType.Value);
        }

        /* Old PreparePacketType(), may be useful in NetPacketEncoder:
         
            var publicProperties = packetInfo.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var packetProperties = publicProperties.Select(property =>
            {
                var propertyAttribute = property.GetCustomAttribute<PacketPropertyAttribute>();
                if (propertyAttribute == null)
                    return null;

                var lengthAttribute = property.GetCustomAttribute<PacketPropertyLengthAttribute>();
                return new PacketPropertyInfo(property, propertyAttribute, lengthAttribute);
            });

            // cache property list for quick access
            var packetPropertyList = packetProperties.Where(x => x != null).ToList();
            packetPropertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            var lengthAttributeInfoList = GetLengthAttributeInfos(packetPropertyList).ToList();
        */

        protected abstract void PreparePacketType(PacketStructInfo structInfo);

        /// <summary>
        /// Validates and returns all property length attributes
        /// together with their sources and targets.
        /// </summary>
        public static IEnumerable<LengthFromAttributeInfo> GetLengthFromAttributeInfos(
            IList<ParameterInfo> parameters)
        {
            for (int targetIndex = 0; targetIndex < parameters.Count; targetIndex++)
            {
                var targetParam = parameters[targetIndex];

                var lengthFromAttrib = targetParam.GetCustomAttribute<LengthFromAttribute>();
                if (lengthFromAttrib == null)
                    continue;

                ParameterInfo sourceParam;
                try
                {
                    sourceParam = parameters[targetIndex + lengthFromAttrib.RelativeIndex];
                }
                catch(ArgumentOutOfRangeException)
                {
                    throw new Exception(
                        string.Format("Relative index for {0} on parameter \"{1}\" is out of range.",
                        nameof(LengthFromAttribute),
                        targetParam.Name));
                }

                yield return new LengthFromAttributeInfo(sourceParam, targetParam);
            }
        }
    }
}
