using System;
using System.Collections.Generic;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    public delegate bool StoreItemPredicate<TSender>(
        TSender sender, Type type, int currentCount);

    /// <summary>
    /// Represents a non-thread-safe pool of <see cref="PacketHolder{T}"/>.
    /// </summary>
    public class PacketHolderPool
    {
        private Dictionary<Type, Stack<PacketHolder>> _items;

        public StoreItemPredicate<PacketHolderPool> StorePacketPredicate { get; }

        public PacketHolderPool(StoreItemPredicate<PacketHolderPool> storePacketPredicate)
        {
            StorePacketPredicate = storePacketPredicate ??
                throw new ArgumentNullException(nameof(storePacketPredicate));

            _items = new Dictionary<Type, Stack<PacketHolder>>();
        }

        public PacketHolder<TPacket> Rent<TPacket>(
            NetPacketWriterAction<TPacket> writer,
            NetConnection connection,
            in TPacket packet)
        {
            // Check for null before renting a holder.
#pragma warning disable IDE0016 // Use 'throw' expression
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
#pragma warning restore IDE0016
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            PacketHolder<TPacket> packetHolder;

            if (_items.TryGetValue(typeof(TPacket), out var stack) &&
                stack.TryPop(out var holder))
            {
                packetHolder = (PacketHolder<TPacket>)holder;
            }
            else
            {
                packetHolder = new PacketHolder<TPacket>();
            }

            packetHolder.Writer = writer;
            packetHolder.Connection = connection;
            packetHolder.State = connection.ProtocolState;
            packetHolder.CompressionThreshold = connection.CompressionThreshold;
            packetHolder.Packet = packet;
            return packetHolder;
        }

        public void Return(PacketHolder holder)
        {
            if (holder == null)
                throw new ArgumentNullException(nameof(holder));

            if (!_items.TryGetValue(holder.PacketType, out var stack))
            {
                stack = new Stack<PacketHolder>();
                _items.Add(holder.PacketType, stack);
            }

            if (StorePacketPredicate(this, holder.PacketType, _items.Count))
                stack.Push(holder);
        }
    }
}

