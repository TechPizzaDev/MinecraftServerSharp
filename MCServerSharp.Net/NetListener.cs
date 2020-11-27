using System;
using System.Net;
using System.Net.Sockets;

namespace MCServerSharp.Net
{
    /// <summary>
    /// Entry point for network connections.
    /// </summary>
    public class NetListener : IDisposable
    {
        public const int CloseTimeout = 10000;

        public delegate void ListenerEvent(NetListener sender);
        public delegate void ConnectionEvent(NetListener sender, NetConnection connection);
        public delegate bool PrimaryConnectionEvent(NetListener sender, NetConnection connection);

        public event ListenerEvent? Started;
        public event ListenerEvent? Stopped;

        public event ConnectionEvent? Connection;
        public event ConnectionEvent? Disconnection;

        private SocketAsyncEventArgs _acceptEvent = new SocketAsyncEventArgs();
        private bool _isDisposed;

        public NetOrchestrator Orchestrator { get; }
        public PrimaryConnectionEvent PrimaryConnectionHandler { get; }
        public Socket Socket { get; }

        public NetListener(NetOrchestrator orchestrator, PrimaryConnectionEvent primaryConnectionHandler)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            PrimaryConnectionHandler = primaryConnectionHandler ?? 
                throw new ArgumentNullException(nameof(primaryConnectionHandler));

            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public void Bind(EndPoint localEndPoint)
        {
            Socket.Bind(localEndPoint);
        }

        public void Start(int backlog)
        {
            _acceptEvent.Completed += (s, e) => ProcessAccept(e);

            Socket.Listen(backlog);
            Started?.Invoke(this);

            StartAccept(_acceptEvent);
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
            var acceptSocket = acceptEvent.AcceptSocket;
            if (acceptSocket == null)
                throw new ArgumentException("The event accept socket is null.", nameof(acceptEvent));

            var connection = new NetConnection(
                Orchestrator,
                acceptSocket,
                closeAction: CloseClientSocket);
          
            // TODO: Use delay when sending initial data, 
            //       then disable delay after initial data has been sent.
            connection.Socket.NoDelay = true;

            connection.Socket.Blocking = true;

            if (PrimaryConnectionHandler.Invoke(this, connection))
            {
                Connection?.Invoke(this, connection);
            }

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

            Disconnection?.Invoke(this, connection);
        }

        public void Stop()
        {
            Socket.Close();
            Stopped?.Invoke(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _acceptEvent.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~NetListener()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
