using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder
    {
        protected Dictionary<Type, PacketStructInfo> RegisteredTypes { get; }
        protected Dictionary<Type, MethodInfo> DataTypes { get; }
        protected Dictionary<Type, Delegate> PreparedPacketCoders { get; }

        public int RegisteredTypeCount => RegisteredTypes.Count;
        public int PreparedTypeCount => PreparedPacketCoders.Count;

        #region Constructors

        public NetPacketCoder()
        {
            RegisteredTypes = new Dictionary<Type, PacketStructInfo>();
            DataTypes = new Dictionary<Type, MethodInfo>();
            PreparedPacketCoders = new Dictionary<Type, Delegate>();
        }

        #endregion

        public Delegate GetPacketCoder(Type packetType)
        {
            if (!PreparedPacketCoders.TryGetValue(packetType, out var reader))
            {
                PreparePacketType(new PacketStructInfo(packetType));
                try
                {
                    reader = PreparedPacketCoders[packetType];
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception($"Missing packet coder for \"{packetType}\".");
                }
            }
            return reader;
        }

        protected void RegisterTypeReaderFromBinary(Type binaryType, string methodName)
        {
            try
            {
                var method = binaryType.GetMethod(methodName, Array.Empty<Type>());
                RegisterDataType(method);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to create data type from {binaryType}.{methodName}().", ex);
            }
        }

        public void RegisterDataType(MethodInfo method)
        {
            if (method == null) 
                throw new ArgumentNullException(nameof(method));

            lock (DataTypes)
                DataTypes.Add(method.ReturnType, method);
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

        protected abstract void PreparePacketType(PacketStructInfo structInfo);
    }
}
