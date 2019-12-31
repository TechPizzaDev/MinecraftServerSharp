using System;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to transforms that turn packets into network messages.
    /// </summary>
    public class NetPacketEncoder : NetPacketCoder
    {
        public delegate void PacketWriterDelegate<in TPacket>(TPacket packet, NetBinaryWriter writer);

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsServerPacket);
        }

        public PacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (PacketWriterDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override void PreparePacketType(PacketStructInfo structInfo)
        {
            if (typeof(IWritablePacket).IsAssignableFrom(structInfo.Type))
            {
                Console.WriteLine("yes xD");
            }
            else
            {
                Console.WriteLine("no xD");
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


        }
    }
}
