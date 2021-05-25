﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCServerSharp.Collections;
using MCServerSharp.Utility;

namespace MCServerSharp.Net.Packets
{
    public abstract partial class NetPacketCoder<TPacketId>
        where TPacketId : unmanaged, Enum
    {
        protected Dictionary<DataTypeKey, MethodInfo> DataTypeHandlers { get; }
        protected Dictionary<Type, Delegate> DataObjectActions { get; }

        protected Dictionary<Type, PacketStructInfo> RegisteredPacketTypes { get; }
        protected Dictionary<Type, Delegate> PacketActions { get; }

        /// <summary>
        /// Array of ID-to-packet mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<int, PacketIdDefinition>[] PacketIdMaps { get; }

        /// <summary>
        /// Array of packet-to-ID mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<Type, PacketIdDefinition>[] PacketTypeToIdMaps { get; }

        public int RegisteredTypeCount => RegisteredPacketTypes.Count;
        public int PreparedTypeCount => PacketActions.Count;

        public NetPacketCoder()
        {
            DataTypeHandlers = new Dictionary<DataTypeKey, MethodInfo>();
            DataObjectActions = new Dictionary<Type, Delegate>();

            RegisteredPacketTypes = new Dictionary<Type, PacketStructInfo>();
            PacketActions = new Dictionary<Type, Delegate>();

            int stateCount = Enum.GetValues(typeof(ProtocolState)).Length;
            PacketIdMaps = new Dictionary<int, PacketIdDefinition>[stateCount];
            PacketTypeToIdMaps = new Dictionary<Type, PacketIdDefinition>[stateCount];
        }

        protected abstract void RegisterDataType(params Type[] arguments);

        #region PacketId-related methods

        public virtual void InitializePacketIdMaps(IEnumerable<FieldInfo> fields)
        {
            var mappingAttributeList = fields
                .SelectWhere(
                f => f.GetCustomAttribute<PacketIdMappingAttribute>(),
                (f, a) => a != null,
                (f, a) => new PacketIdMappingInfo(f, a!))
                .ToList();

            for (int stateIndex = 0; stateIndex < PacketIdMaps.Length; stateIndex++)
            {
                PacketIdMaps[stateIndex] = new Dictionary<int, PacketIdDefinition>();
                PacketTypeToIdMaps[stateIndex] = new Dictionary<Type, PacketIdDefinition>();

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
                            var mapId = EnumConverter.ToEnum<TPacketId>(packetStructAttrib.PacketId);
                            var definition = new PacketIdDefinition(typeEntry.Key, mapRawId, mapId);

                            PacketIdMaps[stateIndex].Add(definition.RawId, definition);
                            PacketTypeToIdMaps[stateIndex].Add(definition.Type, definition);
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
            return PacketTypeToIdMaps[index];
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

        #endregion

        #region PacketAction-related methods

        public abstract Delegate CreatePacketAction(PacketStructInfo structInfo);

        public void CreatePacketActions()
        {
            foreach (var pair in RegisteredPacketTypes)
            {
                var packetAction = CreatePacketAction(pair.Value);
                PacketActions.Add(pair.Value.Type, packetAction);
            }
        }

        public Delegate GetPacketAction(Type packetType)
        {
            if (!PacketActions.TryGetValue(packetType, out var reader))
            {
                reader = CreatePacketAction(new PacketStructInfo(packetType));
                if (reader == null)
                    throw new InvalidOperationException("Failed to create packet action.");
            }
            return reader;
        }

        #endregion
    }
}
