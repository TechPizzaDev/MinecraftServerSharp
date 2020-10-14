
using System;
using System.Collections.Generic;
using System.Reflection;
using MCServerSharp.Collections;

namespace MCServerSharp.Net.Packets
{
    public readonly struct PacketStructInfo
    {
        public Type Type { get; }
        public PacketStructAttribute Attribute { get; }

        public PacketStructInfo(Type type, PacketStructAttribute attribute)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        }

        public PacketStructInfo(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Attribute = type.GetCustomAttribute<PacketStructAttribute>() ??
                throw new ArgumentException(
                    $"Type \"{type.Name}\" is missing packet struct attribute.", nameof(type));
        }

        public static IEnumerable<PacketStructInfo> GetPacketTypes(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var packetTypes = assembly.ExportedTypes.SelectWhere(
                t => t.GetCustomAttribute<PacketStructAttribute>(),
                (t, a) => a != null,
                (t, a) => new PacketStructInfo(t, a!));

            return packetTypes;
        }
    }
}
