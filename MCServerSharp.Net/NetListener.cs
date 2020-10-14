using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace MCServerSharp.Net
{
    /// <summary>
    /// Entry point for network connections.
    /// </summary>
    public class NetListener
    {
        public const int CloseTimeout = 10000;

        public delegate void ListenerEvent(NetListener sender);
        public delegate void ConnectionEvent(NetListener sender, NetConnection connection);
        public delegate bool PrimaryConnectionEvent(NetListener sender, NetConnection connection);

        public event ListenerEvent? Started;
        public event ListenerEvent? Stopped;

        public event ConnectionEvent? Connection;
        public event ConnectionEvent? Disconnection;

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

        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope", 
            Justification = "Async Sockets")]
        public void Start(int backlog)
        {
            var acceptEvent = new SocketAsyncEventArgs();
            acceptEvent.Completed += (s, e) => ProcessAccept(e);

            Socket.Listen(backlog);
            Started?.Invoke(this);

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
            var connection = new NetConnection(
                Orchestrator,
                acceptEvent.AcceptSocket,
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
    }
}
