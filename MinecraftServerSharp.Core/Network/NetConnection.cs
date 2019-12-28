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

        public NetListener Listener { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs SocketEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        public RecyclableMemoryStream MessageBuffer { get; }
        public NetBinaryReader MessageReader { get; }

        public int MessageLength { get; set; } = -1;
        public int MessageLengthBytes { get; set; } = -1;
        public int TotalMessageLength => MessageLength + MessageLengthBytes;

        public ProtocolState State { get; set; } = ProtocolState.Undefined;

        public NetConnection(
            NetListener listener, Socket socket, SocketAsyncEventArgs socketAsyncEvent,
            Action<NetConnection> closeAction)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            SocketEvent = socketAsyncEvent ?? throw new ArgumentNullException(nameof(socketAsyncEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            MessageBuffer = RecyclableMemoryManager.Default.GetStream();
            MessageReader = new NetBinaryReader(MessageBuffer);

            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
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
            MessageBuffer.Seek(length, System.IO.SeekOrigin.Begin);

            // TODO: make better use of these stream instances
            using (var tmp = RecyclableMemoryManager.Default.GetStream(requiredSize: length))
            {
                // Copy all the future data.
                MessageBuffer.PooledCopyTo(tmp);

                // Remove all buffered data.
                MessageBuffer.Capacity = 0;

                // Copy back the future data.
                tmp.WriteTo(MessageBuffer);
                MessageBuffer.Position = 0;
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
