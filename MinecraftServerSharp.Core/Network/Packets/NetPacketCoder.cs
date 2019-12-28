using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinecraftServerSharp.Network.Packets
{
    public abstract partial class NetPacketCoder
    {
        private Dictionary<Type, PacketStructInfo> _registeredTypes;

        public int RegisteredTypeCount => _registeredTypes.Count;
        public int PreparedTypeCount => 0;

        #region Constructors

        public NetPacketCoder()
        {
            _registeredTypes = new Dictionary<Type, PacketStructInfo>();
        }

        #endregion

        public void RegisterPacketTypesFromCallingAssembly(Func<PacketStructInfo, bool> predicate)
        {
            var assembly = Assembly.GetCallingAssembly();
            var packetTypes = PacketStructInfo.GetPacketTypes(assembly);
            RegisterPacketTypes(packetTypes.Where(predicate));
        }

        public void RegisterPacketTypes(IEnumerable<PacketStructInfo> infos)
        {
            foreach (var info in infos)
            {
                RegisterPacketType(info);
            }
        }

        public void RegisterPacketType(PacketStructInfo info)
        {
            _registeredTypes.Add(info.Type, info);
        }

        public void PreparePacketTypes()
        {
            foreach (var registeredType in _registeredTypes)
            {
                PreparePacketType(registeredType.Value);
            }
        }

        private void PreparePacketType(PacketStructInfo packetInfo)
        {
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

            PreparePacketTypeCore(packetInfo, packetPropertyList, lengthAttributeInfoList);
        }

        protected abstract void PreparePacketTypeCore(
            PacketStructInfo packetInfo,
            List<PacketPropertyInfo> propertyList,
            List<PacketPropertyLengthAttributeInfo> lengthAttributeList);

        /// <summary>
        /// Validates and returns all property length attributes
        /// together with their sources and targets.
        /// </summary>
        private static IEnumerable<PacketPropertyLengthAttributeInfo> GetLengthAttributeInfos(
            List<PacketPropertyInfo> packetProperties)
        {
            var propertyNameList = packetProperties.Select(x => x.Name).ToList();

            for (int targetPropertyIndex = 0;
                targetPropertyIndex < packetProperties.Count;
                targetPropertyIndex++)
            {
                var targetProperty = packetProperties[targetPropertyIndex];

                var lengthAttribute = targetProperty.LengthAttribute;
                if (lengthAttribute == null)
                    continue;

                int sourcePropertyIndex = propertyNameList.IndexOf(lengthAttribute.SourcePropertyName);
                if (sourcePropertyIndex == -1)
                    throw new Exception(
                        string.Format("{0} has unknown property name: \"{1}\".",
                        nameof(PacketPropertyLengthAttribute),
                        lengthAttribute.SourcePropertyName));

                var sourceProperty = packetProperties[sourcePropertyIndex];

                if (targetPropertyIndex < sourcePropertyIndex)
                    throw new Exception(
                        string.Format(
                            "The target property \"{0}\" with index {1} preceds " +
                            "the source property \"{2}\" with index {3}.",
                        targetProperty.Name,
                        targetPropertyIndex,
                        sourceProperty.Name,
                        sourcePropertyIndex));

                yield return new PacketPropertyLengthAttributeInfo(sourceProperty, targetProperty);
            }
        }
    }
}
