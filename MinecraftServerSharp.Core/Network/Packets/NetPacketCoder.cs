using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinecraftServerSharp.Network.Packets
{
    public partial class NetPacketCoder
    {
        private static Dictionary<NetTextEncoding, Encoding> _textEncodings;

        private Dictionary<Type, PacketStructInfo> _registeredTypes;

        public int RegisteredTypeCount => _registeredTypes.Count;
        public int PreparedTypeCount => 0;

        #region Constructors

        static NetPacketCoder()
        {
            _textEncodings = new Dictionary<NetTextEncoding, Encoding>
            {
                { NetTextEncoding.Utf8, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) },
                { NetTextEncoding.BigUtf16, new UnicodeEncoding(bigEndian: true, byteOrderMark: false) },
                { NetTextEncoding.BigUtf32, new UTF32Encoding(bigEndian: true, byteOrderMark: false) },
                { NetTextEncoding.LittleUtf16, new UnicodeEncoding(bigEndian: false, byteOrderMark: false) },
                { NetTextEncoding.LittleUtf32, new UTF32Encoding(bigEndian: false, byteOrderMark: false) },
            };
        }

        public NetPacketCoder()
        {
            _registeredTypes = new Dictionary<Type, PacketStructInfo>();
        }

        #endregion

        public void RegisterPacketFromCallingAssembly(Func<PacketStructInfo, bool> predicate)
        {
            var assembly = Assembly.GetCallingAssembly();
            var structTypes = PacketStructInfo.GetPacketTypes(assembly);
            RegisterTypes(structTypes.Where(predicate));
        }

        public void RegisterTypes(IEnumerable<PacketStructInfo> infos)
        {
            foreach (var info in infos)
            {
                RegisterType(info);
            }
        }

        public void RegisterType(PacketStructInfo info)
        {
            _registeredTypes.Add(info.Type, info);
        }

        public void PrepareTypes()
        {
            foreach (var registeredType in _registeredTypes)
            {
                PrepareType(registeredType.Value);
            }
        }

        private void PrepareType(PacketStructInfo info)
        {
            var publicProperties = info.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var packetProperties = publicProperties.Select(x =>
            {
                var propertyAttribute = x.GetCustomAttribute<PacketPropertyAttribute>();
                if (propertyAttribute == null)
                    return null;

                var lengthAttribute = x.GetCustomAttribute<PacketPropertyLengthAttribute>();
                return new PacketPropertyInfo(x, propertyAttribute, lengthAttribute);
            });

            // cache list for quick access
            var packetPropertyList = packetProperties.Where(x => x != null).ToList();
            packetPropertyList.Sort((x, y) => x.SerializationOrder.CompareTo(y.SerializationOrder));

            var lengthAttributeInfos = GetLengthAttributeInfos(packetPropertyList);

            Console.WriteLine("todo");
        }

        /// <summary>
        /// Validates and returns all property length attributes together with their targets.
        /// </summary>
        private static IEnumerable<PropertyLengthAttributeInfo> GetLengthAttributeInfos(
            List<PacketPropertyInfo> packetProperties)
        {
            var propertyNameList = packetProperties.Select(x => x.PropertyInfo.Name).ToList();

            foreach (var targetProperty in packetProperties)
            {
                var lengthAttribute = targetProperty.LengthAttribute;
                if (lengthAttribute == null)
                    continue;

                int lengthPropertyIndex = propertyNameList.IndexOf(lengthAttribute.LengthPropertyName);
                if (lengthPropertyIndex == -1)
                    throw new Exception(
                        string.Format("{0} has unknown property name: \"{1}\".",
                        nameof(PacketPropertyLengthAttribute),
                        lengthAttribute.LengthPropertyName));

                var sourceProperty = packetProperties[lengthPropertyIndex];
                yield return new PropertyLengthAttributeInfo(sourceProperty, targetProperty);
            }
        }
    }
}
