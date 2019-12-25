using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpMinecraftServer.Network;

namespace SharpMinecraftServer
{
    internal class Program
    {
        private static NetProcessor _processor;

        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            _processor = new NetProcessor();

            var listener = new NetListener(new IPEndPoint(IPAddress.Any, 25565));
            listener.Connection += Listener_Connection;
            listener.Disconnection += Listener_Disconnection;

            listener.Start(backlog: 100);

            Console.ReadKey();
            return;

            Console.WriteLine("kek");
            Thread.Sleep(1000);

            for (int i = 0; i < 100; i++)
            {
                var t = new Thread(() =>
                {
                    var c = new TcpClient();
                    c.Connect(new IPEndPoint(IPAddress.Loopback, 25565));
                    while (true)
                    {
                        c.Client.Send(Encoding.UTF8.GetBytes("very kek"));
                        Thread.Sleep(50);
                    }
                });
                t.Start();
            }
        }

        private static void Listener_Connection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Connection: " + connection.RemoteEndPoint);
            //Task.Run(() => PlayerConnectionLoop(connection));

            _processor.AddConnection(connection);
        }

        private static void Listener_Disconnection(NetListener sender, NetConnection connection)
        {
            Console.WriteLine("Disconnection: " + connection.RemoteEndPoint);
        }
    }
}
