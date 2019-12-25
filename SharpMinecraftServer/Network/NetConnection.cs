using System;
using System.Net;
using System.Net.Sockets;

namespace SharpMinecraftServer.Network
{
    public class NetConnection
    {
        private Action<NetConnection> _closeAction;

        public NetListener Listener { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs SocketEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        public NetConnection(
            NetListener listener, Socket socket, SocketAsyncEventArgs socketAsyncEvent,
            Action<NetConnection> closeAction)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            SocketEvent = socketAsyncEvent ?? throw new ArgumentNullException(nameof(socketAsyncEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        public void Close()
        {
            if (_closeAction == null)
                throw new InvalidOperationException();

            _closeAction.Invoke(this);
            _closeAction = null;
        }
    }
}
