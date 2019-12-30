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
