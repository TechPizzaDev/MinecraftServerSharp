using System;
using System.Collections.Generic;
using System.Net;
using MinecraftServerSharp.Collections;

namespace MinecraftServerSharp.Network
{
    public class NetManager
    {
        public object ConnectionMutex { get; } = new object();
        private HashSet<NetConnection> _connections;

        public NetProcessor Processor { get; }
        public NetOrchestrator Orchestrator { get; }
        public NetListener Listener { get; }

        public ReadOnlySet<NetConnection> Connections { get; }

        public NetManager()
        {
            Processor = new NetProcessor();
            Orchestrator = new NetOrchestrator(Processor);
            Listener = new NetListener(Orchestrator);

            _connections = new HashSet<NetConnection>();
            Connections = _connections.AsReadOnly();
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            Listener.Bind(localEndPoint);
        }

        public void Setup()
        {
            Processor.SetupCodecs();
        }

        public void Listen(int backlog)
        {
            Orchestrator.Start(workerCount: 1); // TODO: fix concurrency
            
            Listener.Connection += Listener_Connection;
            Listener.Disconnection += Listener_Disconnection;

            Listener.Start(backlog);
        }

        public void Flush()
        {
            Orchestrator.Flush();
        }

        private void Listener_Connection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Add(connection))
                    throw new InvalidOperationException();
            }

            Processor.AddConnection(connection);
        }

        private void Listener_Disconnection(NetListener sender, NetConnection connection)
        {
            lock (ConnectionMutex)
            {
                if (!_connections.Remove(connection))
                    throw new InvalidOperationException();
            }

        }

        public int getConnectionAmount()
        {
            lock (ConnectionMutex)
            {
                return _connections.Count;
            }
    }
}
