using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCodec<TPacketId>
        where TPacketId : Enum
    {
        protected Dictionary<DataTypeKey, MethodInfo> DataTypeHandlers { get; }
        protected Dictionary<Type, PacketStructInfo> RegisteredPacketTypes { get; }
        protected Dictionary<Type, Delegate> PacketCodecDelegates { get; }

        /// <summary>
        /// Array of ID-to-packet mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<int, PacketIdDefinition>[] PacketIdMaps { get; }

        /// <summary>
        /// Array of packet-to-ID mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<Type, PacketIdDefinition>[] TypeToPacketIdMaps { get; }

        public int RegisteredTypeCount => RegisteredPacketTypes.Count;
        public int PreparedTypeCount => PacketCodecDelegates.Count;

        public NetPacketCodec()
        {
            DataTypeHandlers = new Dictionary<DataTypeKey, MethodInfo>();
            RegisteredPacketTypes = new Dictionary<Type, PacketStructInfo>();
            PacketCodecDelegates = new Dictionary<Type, Delegate>();

            int stateCount = Enum.GetValues(typeof(ProtocolState)).Length;
            PacketIdMaps = new Dictionary<int, PacketIdDefinition>[stateCount];
            TypeToPacketIdMaps = new Dictionary<Type, PacketIdDefinition>[stateCount];
        }

        protected abstract void RegisterDataType(params Type[] arguments);

        #region PacketId-related methods

        public virtual void InitializePacketIdMaps()
        {
            var fields = typeof(TPacketId).GetFields();
            var mappingAttributeList = fields
                .Where(f => f.GetCustomAttribute<PacketIdMappingAttribute>() != null)
                .Select(f => new PacketIdMappingInfo(f, f.GetCustomAttribute<PacketIdMappingAttribute>()!))
                .ToList();
            
            for (int stateIndex = 0; stateIndex < PacketIdMaps.Length; stateIndex++)
            {
                PacketIdMaps[stateIndex] = new Dictionary<int, PacketIdDefinition>();
                TypeToPacketIdMaps[stateIndex] = new Dictionary<Type, PacketIdDefinition>();

                var state = (ProtocolState)stateIndex;
                foreach (var mappingInfo in mappingAttributeList.Where(x => x.Attribute.State == state))
                {
                    foreach (var typeEntry in RegisteredPacketTypes)
                    {
                        var packetStructAttrib = typeEntry.Value.Attribute;
                        var enumValue = mappingInfo.Field.GetRawConstantValue();
                        if (packetStructAttrib.PacketId.Equals(enumValue))
                        {
                            var mapRawId = mappingInfo.Attribute.RawId;
                            var mapId = EnumConverter<TPacketId>.Convert(packetStructAttrib.PacketId);
                            var definition = new PacketIdDefinition(typeEntry.Key, mapRawId, mapId);

                            PacketIdMaps[stateIndex].Add(definition.RawId, definition);
                            TypeToPacketIdMaps[stateIndex].Add(definition.Type, definition);
                        }
                    }
                }
            }
        }

        protected Dictionary<int, PacketIdDefinition> GetPacketIdMap(ProtocolState state)
        {
            int index = (int)state;
            return PacketIdMaps[index];
        }

        protected Dictionary<Type, PacketIdDefinition> GetTypeToPacketIdMap(ProtocolState state)
        {
            int index = (int)state;
            return TypeToPacketIdMaps[index];
        }

        public bool TryGetPacketIdDefinition(
            ProtocolState state, int rawId, out PacketIdDefinition definition)
        {
            var map = GetPacketIdMap(state);
            return map.TryGetValue(rawId, out definition);
        }

        public bool TryGetPacketIdDefinition(
            ProtocolState state, Type packetType, out PacketIdDefinition definition)
        {
            var map = GetTypeToPacketIdMap(state);
            return map.TryGetValue(packetType, out definition);
        }

        public bool TryGetPacketIdDefinition(TPacketId id, out PacketIdDefinition definition)
        {
            for (int i = 0; i < PacketIdMaps.Length; i++)
            {
                var map = GetPacketIdMap((ProtocolState)i);
                foreach (var value in map.Values)
                {
                    if (EqualityComparer<TPacketId>.Default.Equals(value.Id, id))
                    {
                        definition = value;
                        return true;
                    }
                }
            }
            definition = default;
            return false;
        }

        #endregion

        #region RegisterDataType[FromMethod]

        protected void RegisterDataTypeFromMethod(
            Type type, string methodName, params Type[] arguments)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                if (arguments == null)
                    arguments = Array.Empty<Type>();

                var method = type.GetMethod(methodName, arguments);
                if (method == null)
                    throw new Exception($"Could not find method \"{methodName}\"({arguments.ToListString()}).");

                RegisterDataType(method);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create data type from method {type}.{methodName}({arguments.ToListString()}).", ex);
            }
        }

        public void RegisterDataType(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var paramTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            lock (DataTypeHandlers)
                DataTypeHandlers.Add(new DataTypeKey(method.ReturnType, paramTypes), method);
        }

        #endregion

        #region RegisterPacketType[s]

        public void RegisterPacketType(PacketStructInfo info)
        {
            RegisteredPacketTypes.Add(info.Type, info);
        }

        public void RegisterPacketTypes(IEnumerable<PacketStructInfo> infos)
        {
            if (infos == null)
                throw new ArgumentNullException(nameof(infos));

            foreach (var info in infos)
                RegisterPacketType(info);
        }

        public void RegisterPacketTypesFromCallingAssembly(Func<PacketStructInfo, bool> predicate)
        {
            var assembly = Assembly.GetCallingAssembly();
            var packetTypes = PacketStructInfo.GetPacketTypes(assembly);
            RegisterPacketTypes(packetTypes.Where(predicate));
        }

        #endregion

        #region CoderDelegate-related methods

        protected abstract Delegate CreateCodecDelegate(PacketStructInfo structInfo);

        public void CreateCodecDelegates()
        {
            foreach (var pair in RegisteredPacketTypes)
            {
                var codecDelegate = CreateCodecDelegate(pair.Value);
                PacketCodecDelegates.Add(pair.Value.Type, codecDelegate);
            }
        }

        public Delegate GetPacketCodec(Type packetType)
        {
            if (!PacketCodecDelegates.TryGetValue(packetType, out var reader))
            {
                CreateCodecDelegate(new PacketStructInfo(packetType));
                try
                {
                    reader = PacketCodecDelegates[packetType];
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception($"Missing packet codec for \"{packetType}\".");
                }
            }
            return reader;
        }

        #endregion
    }
}
