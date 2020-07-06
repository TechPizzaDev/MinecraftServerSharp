using System;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
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
            var gameTicker = new Ticker(targetTickTime: TimeSpan.FromMilliseconds(50));

            var manager = new NetManager();
            manager.Listener.Connection += Manager_Connection;
            manager.Listener.Disconnection += Manager_Disconnection;

            ushort port = 25565;
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            manager.Bind(localEndPoint);
            Console.WriteLine("Listener bound to endpoint " + localEndPoint);

            Console.WriteLine("Setting up network manager...");
            manager.Setup();

            int backlog = 200;
            Console.WriteLine("Listener backlog queue size: " + backlog);

            manager.Listen(backlog);
            Console.WriteLine("Listening for connections...");

            int tickCount = 0;
            var rng = new Random();

            gameTicker.Tick += (ticker) =>
            {
                tickCount++;
                if (tickCount % 10 == 0)
                {
                    //Console.WriteLine(
                    //    "Tick Time: " +
                    //    ticker.ElapsedTime.TotalMilliseconds.ToString("00.00") +
                    //    "/" +
                    //    ticker.TargetTime.TotalMilliseconds.ToString("00") + " ms" +
                    //    " | " +
                    //    (ticker.ElapsedTime.Ticks / (float)ticker.TargetTime.Ticks * 100f).ToString("00.0") + "%");

                    lock (manager.ConnectionMutex)
                    {
                        int count = manager.Connections.Count;
                        if (count > 0)
                            Console.WriteLine(count + " connections");
                    }
                }

                //world.Tick();
                manager.Flush();
            };
            gameTicker.Run();
            
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
