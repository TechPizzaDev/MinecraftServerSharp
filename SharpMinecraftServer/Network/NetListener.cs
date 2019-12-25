using System;
using System.Net;
using System.Net.Sockets;

namespace SharpMinecraftServer.Network
{
    /// <summary>
    /// Entry point for network connections.
    /// </summary>
    public class NetListener
    {
        public delegate void ListenerEvent(NetListener sender);
        public delegate void ConnectionEvent(NetListener sender, NetConnection connection);

        private Socket _listener;

        public event ListenerEvent Started;
        public event ListenerEvent Stopped;

        public event ConnectionEvent Connection;
        public event ConnectionEvent Disconnection;

        public NetListener(IPEndPoint localEndPoint)
        {
            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(localEndPoint);
        }

        public void Start(int backlog)
        {
            _listener.Listen(backlog);
            Started?.Invoke(this);

            var acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += (s, e) => ProcessAccept(e);

            StartAccept(acceptEventArg);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            // socket must be cleared since the context object is being reused
            acceptEventArg.AcceptSocket = null;

            if (!_listener.AcceptAsync(acceptEventArg))
                ProcessAccept(acceptEventArg);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            var readEventArgs = new SocketAsyncEventArgs(); // TODO: pool 
            var connection = new NetConnection(
                this, e.AcceptSocket, readEventArgs, closeAction: CloseClientSocket);
            
            byte[] buffer = new byte[4096]; // TODO: pool
            readEventArgs.SetBuffer(buffer, 0, buffer.Length);

            connection.Socket.NoDelay = true;
            readEventArgs.UserToken = connection;

            // TODO: do some validation
            Connection?.Invoke(this, connection);

            // Accept the next connection request
            StartAccept(e);
        }

        private void CloseClientSocket(NetConnection connection)
        {
            try
            {
                connection.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) // throws if client process has already closed
            {
            }

            connection.Socket.Close();

            // m_readWritePool.Push(e); // TODO: pool

            Disconnection?.Invoke(this, connection);
        }

        public void Stop()
        {
            _listener.Close();
            Stopped?.Invoke(this);
        }
    }
}
