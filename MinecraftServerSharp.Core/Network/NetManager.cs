using System.Net;

namespace MinecraftServerSharp.Network
{
    public class NetManager
    {
        public NetProcessor Processor { get; }
        public NetOrchestrator Orchestrator { get; }
        public NetListener Listener { get; }

        public NetManager()
        {
            Processor = new NetProcessor();
            Orchestrator = new NetOrchestrator(Processor);
            Listener = new NetListener(Orchestrator);
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
            Orchestrator.Start(workerCount: 2);

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
            Processor.AddConnection(connection);
        }

        private void Listener_Disconnection(NetListener sender, NetConnection connection)
        {
        }
    }
}
