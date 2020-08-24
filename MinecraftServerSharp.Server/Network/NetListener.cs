using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace MinecraftServerSharp.Net
{
    /// <summary>
    /// Entry point for network connections.
    /// </summary>
    public class NetListener
    {
        public const int CloseTimeout = 10000;

        public delegate void ListenerEvent(NetListener sender);
        public delegate void ConnectionEvent(NetListener sender, NetConnection connection);

        public event ListenerEvent? Started;
        public event ListenerEvent? Stopped;

        public event ConnectionEvent? Connection;
        public event ConnectionEvent? Disconnection;

        private NetOrchestrator Orchestrator { get; }
        public Socket Socket { get; }

        public NetListener(NetOrchestrator orchestrator)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public void Bind(EndPoint localEndPoint)
        {
            Socket.Bind(localEndPoint);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Async Sockets")]
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
            if (acceptEvent == null)
                throw new ArgumentNullException(nameof(acceptEvent));

            // clear since the context is reused
            acceptEvent.AcceptSocket = null;

            if (!Socket.AcceptAsync(acceptEvent))
                ProcessAccept(acceptEvent);
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEvent)
        {
            var receiveEvent = new SocketAsyncEventArgs(); // TODO: pool 
            var sendEvent = new SocketAsyncEventArgs(); // TODO: pool 

            var connection = new NetConnection(
                Orchestrator,
                acceptEvent.AcceptSocket,
                receiveEvent,
                sendEvent,
                closeAction: CloseClientSocket);
            
            var receiveBuffer = new byte[4096];
            receiveEvent.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

            receiveEvent.UserToken = connection;
            sendEvent.UserToken = connection;

            // TODO: Use delay when sending initial data, 
            // then disable delay after initial data has been sent.
            connection.Socket.NoDelay = true;

            // TODO: do some validation of the client
            Connection?.Invoke(this, connection);

            // Accept the next connection request
            StartAccept(acceptEvent);
        }

        private void CloseClientSocket(NetConnection connection)
        {
            try
            {
                connection.Socket.Close(CloseTimeout);
            }
            catch (Exception) // throws if client process has already closed
            {
            }

            // _readWritePool.Push(e); // TODO: pool

            Disconnection?.Invoke(this, connection);
        }

        public void Stop()
        {
            Socket.Close();
            Stopped?.Invoke(this);
        }
    }
}
