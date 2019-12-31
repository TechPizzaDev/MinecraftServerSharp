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
        public const int CloseTimeout = 10000;

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

        public void Start(int backlog, NetManager manager)
        {
            Socket.Listen(backlog);
            Started?.Invoke(this);

            var acceptEvent = new SocketAsyncEventArgs();
            acceptEvent.Completed += (s, e) => ProcessAccept(e);
            acceptEvent.UserToken = manager;

            StartAccept(acceptEvent);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEvent)
        {
            // clear since the context is reused
            acceptEvent.AcceptSocket = null;

            if (!Socket.AcceptAsync(acceptEvent))
                ProcessAccept(acceptEvent);
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEvent)
        {
            var receiveEvent = new SocketAsyncEventArgs(); // TODO: pool 
            byte[] receiveBuffer = new byte[4096];
            receiveEvent.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

            var sendEvent = new SocketAsyncEventArgs(); // TODO: pool 

            var connection = new NetConnection(
                (NetManager)acceptEvent.UserToken,
                acceptEvent.AcceptSocket,
                receiveEvent,
                sendEvent,
                closeAction: CloseClientSocket);

            connection.Socket.NoDelay = true;

            receiveEvent.UserToken = connection;
            sendEvent.UserToken = connection;

            // TODO: do some validation
            Connection?.Invoke(this, connection);

            // Accept the next connection request
            StartAccept(acceptEvent);
        }

        private void CloseClientSocket(NetConnection connection)
        {
            try
            {
                connection.Socket.Shutdown(SocketShutdown.Both);
                connection.Socket.Close(CloseTimeout);
            }
            catch (Exception) // throws if client process has already closed
            {
            }

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
