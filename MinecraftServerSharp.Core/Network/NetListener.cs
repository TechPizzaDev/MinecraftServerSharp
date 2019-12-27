using System;
using System.Net;
using System.Net.Sockets;

namespace MinecraftServerSharp.Network
{
    /// <summary>
    /// Entry point for network connections.
    /// </summary>
    public class NetListener
    {
        public delegate void ListenerEvent(NetListener sender);
        public delegate void ConnectionEvent(NetListener sender, NetConnection connection);

        public event ListenerEvent Started;
        public event ListenerEvent Stopped;

        public event ConnectionEvent Connection;
        public event ConnectionEvent Disconnection;

        public Socket Socket { get; }

        public NetListener()
        {
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            Socket.Bind(localEndPoint);
        }

        public void Start(int backlog)
        {
            Socket.Listen(backlog);
            Started?.Invoke(this);

            var acceptEvent = new SocketAsyncEventArgs();
            acceptEvent.Completed += (s, e) => ProcessAccept(e);

            StartAccept(acceptEvent);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEvent)
        {
            // socket must be cleared since the context object is being reused
            acceptEvent.AcceptSocket = null;

            if (!Socket.AcceptAsync(acceptEvent))
                ProcessAccept(acceptEvent);
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEvent)
        {
            var connectionEvent = new SocketAsyncEventArgs(); // TODO: pool 
            var connection = new NetConnection(
                this, acceptEvent.AcceptSocket, connectionEvent, closeAction: CloseClientSocket);
            
            byte[] buffer = new byte[4096]; // TODO: pool
            connectionEvent.SetBuffer(buffer, 0, buffer.Length);

            connection.Socket.NoDelay = true;
            connectionEvent.UserToken = connection;

            // TODO: do some validation
            Connection?.Invoke(this, connection);

            // Accept the next connection request
            StartAccept(acceptEvent);
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
            Socket.Close();
            Stopped?.Invoke(this);
        }
    }
}
