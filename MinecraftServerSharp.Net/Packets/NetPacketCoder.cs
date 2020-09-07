using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net.Packets
{
    public abstract partial class NetPacketCoder<TPacketId>
        where TPacketId : Enum
    {
        protected Dictionary<DataTypeKey, MethodInfo> DataTypeHandlers { get; }
        protected Dictionary<Type, PacketStructInfo> RegisteredPacketTypes { get; }
        protected Dictionary<Type, Delegate> PacketCoderDelegates { get; }

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
        public int PreparedTypeCount => PacketCoderDelegates.Count;

        public NetPacketCoder()
        {
            DataTypeHandlers = new Dictionary<DataTypeKey, MethodInfo>();
            RegisteredPacketTypes = new Dictionary<Type, PacketStructInfo>();
            PacketCoderDelegates = new Dictionary<Type, Delegate>();

            int stateCount = Enum.GetValues(typeof(ProtocolState)).Length;
            PacketIdMaps = new Dictionary<int, PacketIdDefinition>[stateCount];
            TypeToPacketIdMaps = new Dictionary<Type, PacketIdDefinition>[stateCount];
        }

        protected abstract void RegisterDataType(params Type[] arguments);

        #region PacketId-related methods

        public virtual void InitializePacketIdMaps(IEnumerable<FieldInfo> fields)
        {
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
            Type[] types, string methodName, params Type[] arguments)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));

            if (arguments == null)
                arguments = Array.Empty<Type>();

            MethodInfo? method = null;

            foreach (var type in types)
            {
                method = type.GetMethod(methodName, arguments);
                if (method != null)
                    break;
            }

            if (method == null)
                throw new Exception($"Could not find method \"{methodName}\"({arguments.ToListString()}).");

            RegisterDataType(method);
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

        public void RegisterLoopbackPacketTypes(Assembly assembly)
        {
            var loopbackFields = new HashSet<TPacketId>(typeof(TPacketId).GetFields().Where(x =>
            {
                var attrib = x.GetCustomAttribute<PacketIdMappingAttribute>();
                return attrib != null && attrib.State == ProtocolState.Loopback;
            }).Select(x => (TPacketId)(x.GetRawConstantValue() ?? 0)));

            var packetTypes = PacketStructInfo.GetPacketTypes(assembly);
            RegisterPacketTypes(packetTypes.Where(
                x => loopbackFields.Contains(EnumConverter<TPacketId>.Convert(x.Attribute.PacketId))));
        }

        #endregion

        #region CoderDelegate-related methods

        protected abstract Delegate CreateCoderDelegate(PacketStructInfo structInfo);

        public void CreateCoderDelegates()
        {
            foreach (var pair in RegisteredPacketTypes)
            {
                var coderDelegate = CreateCoderDelegate(pair.Value);
                PacketCoderDelegates.Add(pair.Value.Type, coderDelegate);
            }
        }

        public Delegate GetPacketCoder(Type packetType)
        {
            if (!PacketCoderDelegates.TryGetValue(packetType, out var reader))
            {
                CreateCoderDelegate(new PacketStructInfo(packetType));
                try
                {
                    reader = PacketCoderDelegates[packetType];
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception($"Missing packet coder for \"{packetType}\".");
                }
            }
            return reader;
        }

        #endregion
    }
}
