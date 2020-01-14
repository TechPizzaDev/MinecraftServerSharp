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
        private const int MaxPacketIdMapIndex = 5;

        protected Dictionary<Type, PacketStructInfo> RegisteredTypes { get; }
        protected Dictionary<DataTypeKey, MethodInfo> DataTypes { get; }
        protected Dictionary<Type, Delegate> PacketCoderDelegates { get; }

        /// <summary>
        /// Array of ID-to-packet type mappings,
        /// indexed by the integer value of <see cref="ProtocolState"/>.
        /// </summary>
        protected Dictionary<int, PacketIdDefinition>[] PacketIdMaps { get; }

        public int RegisteredTypeCount => RegisteredTypes.Count;
        public int PreparedTypeCount => PacketCoderDelegates.Count;

        #region Constructors

        public NetPacketCoder()
        {
            RegisteredTypes = new Dictionary<Type, PacketStructInfo>();
            DataTypes = new Dictionary<DataTypeKey, MethodInfo>();
            PacketCoderDelegates = new Dictionary<Type, Delegate>();

            PacketIdMaps = new Dictionary<int, PacketIdDefinition>[MaxPacketIdMapIndex + 1];
            InitializePacketIdMap();
        }

        #endregion

        protected virtual void InitializePacketIdMap()
        {
            var members = typeof(TPacketID).GetMembers();
            Console.WriteLine(members);
        }

        protected Dictionary<int, PacketIdDefinition> GetPacketIdMap(ProtocolState state)
        {
            int index = (int)state;
            if (index < 0 || index > MaxPacketIdMapIndex)
                throw new ArgumentOutOfRangeException(nameof(state));
            return PacketIdMaps[index];
        }

        public bool TryGetPacketIdDefinition(
            ProtocolState state, int rawId, out PacketIdDefinition definition)
        {
            var map = GetPacketIdMap(state);
            return map.TryGetValue(rawId, out definition);
        }

        #region RegisterDataType[From]

        protected void RegisterDataTypeFrom(
            Type type, string methodName, Type[] arguments = null)
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

        #endregion

        #region Coder Delegate methods

        protected abstract Delegate CreateCoderDelegate(PacketStructInfo structInfo);

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

        public void CreateCoderDelegates()
        {
            foreach (var pair in RegisteredTypes)
            {
                var coderDelegate = CreateCoderDelegate(pair.Value);
                PacketCoderDelegates.Add(pair.Value.Type, coderDelegate);
            }
        }

        #endregion
    }
}
