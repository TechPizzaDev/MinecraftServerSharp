using System;
using System.Net;
using MinecraftServerSharp.Network;

namespace MinecraftServerSharp
{
    public class World
    {

    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var ticker = new Ticker();

            var manager = new NetManager();
            manager.Listener.Connection += Manager_Connection;
            manager.Listener.Disconnection += Manager_Disconnection;

            ushort port = 25565;
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            manager.Bind(localEndPoint);
            Console.WriteLine("Listener bound to endpoint " + localEndPoint);

            Console.WriteLine("Setting up network manager...");
            manager.Setup();

            int backlog = 100;
            Console.WriteLine("Listener backlog queue size: " + backlog);

            manager.Listen(backlog);
            Console.WriteLine("Listening for connections...");

            ticker.Tick += (ticker) =>
            {
                //world.Tick();
                manager.Flush();
            };
            ticker.Run();

            Console.ReadKey();
            return;
        }

        private static void Manager_Connection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Connection: " + connection.RemoteEndPoint);
        }

        private static void Manager_Disconnection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Disconnection: " + connection.RemoteEndPoint);
        }
    }
}
