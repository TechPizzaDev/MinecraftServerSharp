
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MinecraftServerSharp.Network.Packets
{
    public readonly struct PacketStructInfo
    {
        public Type Type { get; }
        public PacketStructAttribute Attribute { get; }

        public bool IsClientPacket => Attribute.ClientPacketID != ClientPacketID.Undefined;
        public bool IsServerPacket => Attribute.ServerPacketID != ServerPacketID.Undefined;

        public PacketStructInfo(Type type, PacketStructAttribute attribute)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        }

        public static IEnumerable<PacketStructInfo> GetPacketTypes(Assembly assembly)
        {
            foreach (Type type in assembly.ExportedTypes)
            {
                var structAttribute = type.GetCustomAttribute<PacketStructAttribute>();
                if (structAttribute != null)
                {
                    yield return new PacketStructInfo(type, structAttribute);
                }
            }
        }
    }
}
