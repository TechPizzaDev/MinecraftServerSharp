using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder<TPacketID>
        where TPacketID : Enum
    {
        protected Dictionary<DataTypeKey, MethodInfo> DataTypes { get; }
        protected Dictionary<Type, PacketStructInfo> RegisteredPacketTypes { get; }
        protected Dictionary<Type, Delegate> PacketCoderDelegates { get; }

        /// <summary>
        /// Array of ID-to-packet mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<int, PacketIdDefinition>[] PacketIdMaps { get; }

        public int RegisteredTypeCount => RegisteredPacketTypes.Count;
        public int PreparedTypeCount => PacketCoderDelegates.Count;

        public NetPacketCoder()
        {
            DataTypes = new Dictionary<DataTypeKey, MethodInfo>();
            RegisteredPacketTypes = new Dictionary<Type, PacketStructInfo>();
            PacketCoderDelegates = new Dictionary<Type, Delegate>();
            PacketIdMaps = new Dictionary<int, PacketIdDefinition>[(int)ProtocolState.MAX];
        }

        protected abstract void RegisterDataType(params Type[] arguments);

        #region PacketId-related methods

        public virtual void InitializePacketIdMaps()
        {
            var fields = typeof(TPacketID).GetFields();
            var mappingAttributeList = fields
                .Where(f => f.GetCustomAttribute<PacketIDMappingAttribute>() != null)
                .Select(f => new PacketIDMappingInfo(f, f.GetCustomAttribute<PacketIDMappingAttribute>()))
                .ToList();

            for (int i = 0; i < PacketIdMaps.Length; i++)
            {
                PacketIdMaps[i] = new Dictionary<int, PacketIdDefinition>();
                var state = (ProtocolState)i;

                foreach (var mappingInfo in mappingAttributeList.Where(x => x.Attribute.State == state))
                {
                    foreach (var typeEntry in RegisteredPacketTypes)
                    {
                        var packetStructAttrib = typeEntry.Value.Attribute;
                        var enumValue = mappingInfo.Field.GetRawConstantValue();
                        if (packetStructAttrib.PacketID.Equals(enumValue))
                        {
                            var mapRawID = mappingInfo.Attribute.RawID;
                            var mapID = EnumConverter<TPacketID>.Convert(packetStructAttrib.PacketID);
                            PacketIdMaps[i].Add(mapRawID, new PacketIdDefinition(typeEntry.Key, mapRawID, mapID));
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

        public bool TryGetPacketIdDefinition(
            ProtocolState state, int rawId, out PacketIdDefinition definition)
        {
            var map = GetPacketIdMap(state);
            return map.TryGetValue(rawId, out definition);
        }

        public bool TryGetPacketIdDefinition(TPacketID id, out PacketIdDefinition definition)
        {
            for (int i = 0; i < PacketIdMaps.Length; i++)
            {
                var map = GetPacketIdMap((ProtocolState)i);
                foreach (var value in map.Values)
                {
                    if (EqualityComparer<TPacketID>.Default.Equals(value.ID, id))
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
            lock (DataTypes)
                DataTypes.Add(new DataTypeKey(method.ReturnType, paramTypes), method);
        }

        #endregion

        #region RegisterPacketType[s]

        public void RegisterPacketType(PacketStructInfo info)
        {
            RegisteredPacketTypes.Add(info.Type, info);
        }

        public void RegisterPacketTypes(IEnumerable<PacketStructInfo> infos)
        {
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
