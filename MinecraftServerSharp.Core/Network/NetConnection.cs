using System;
using System.Net;
using System.Net.Sockets;
using MinecraftServerSharp.Network.Data;
using MinecraftServerSharp.Network.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Network
{
    public class NetConnection
    {
        private Action<NetConnection> _closeAction;
        private NetPacketDecoder _packetDecoder; // cached from NetManager

        public NetManager Manager { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs SocketEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        public RecyclableMemoryStream Buffer { get; }
        public NetBinaryReader Reader { get; }

        public int MessageLength { get; set; } = -1;
        public int MessageLengthBytes { get; set; } = -1;
        public int TotalMessageLength => MessageLength + MessageLengthBytes;

        public ProtocolState State { get; set; } = ProtocolState.Undefined;

        public NetConnection(
            NetManager manager,
            Socket socket,
            SocketAsyncEventArgs socketAsyncEvent,
            Action<NetConnection> closeAction)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            SocketEvent = socketAsyncEvent ?? throw new ArgumentNullException(nameof(socketAsyncEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            Buffer = RecyclableMemoryManager.Default.GetStream();
            Reader = new NetBinaryReader(Buffer);

            _packetDecoder = Manager.Processor.PacketDecoder;

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        public TPacket ReadPacket<TPacket>()
        {
            var reader = _packetDecoder.GetPacketReader<TPacket>();
            return reader.Invoke(Reader);
        }

        /// <summary>
        /// Removes the current message from the buffer
        /// while keeping all the to-be-processed data.
        /// </summary>
        public void TrimCurrentMessage()
        {
            int offset = TotalMessageLength;
            TrimMessageBuffer(offset);
        }

        /// <summary>
        /// Removes a front portion of the buffer.
        /// </summary>
        public void TrimMessageBuffer(int length)
        {
            if (length == 0)
                return;

            // Seek past the data.
            Buffer.Seek(length, System.IO.SeekOrigin.Begin);

            // TODO: make better use of these stream instances
            using (var tmp = RecyclableMemoryManager.Default.GetStream(requiredSize: length))
            {
                // Copy all the future data.
                Buffer.PooledCopyTo(tmp);

                // Remove all buffered data.
                Buffer.Capacity = 0;

                // Copy back the future data.
                tmp.WriteTo(Buffer);
                Buffer.Position = 0;
            }

            MessageLength = -1;
            MessageLengthBytes = -1;
        }

        public bool Close()
        {
            if (_closeAction == null)
                return false;

            _closeAction.Invoke(this);
            _closeAction = null;
            return true;
        }
    }
}
