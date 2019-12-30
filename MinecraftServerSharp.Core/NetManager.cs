using System.Net;
using MinecraftServerSharp.Network;

namespace MinecraftServerSharp
{
    public class NetManager
    {
        public NetProcessor Processor { get; }
        public NetListener Listener { get; }

        public NetManager()
        {
            Processor = new NetProcessor();
            Listener = new NetListener();
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            Listener.Bind(localEndPoint);
        }

        public void Setup()
        {
            Processor.SetupCoders();
        }

        public void Listen(int backlog)
        {
            Listener.Connection += Listener_Connection;
            Listener.Disconnection += Listener_Disconnection;

            Listener.Start(backlog, this);
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
