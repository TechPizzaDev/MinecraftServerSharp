using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMinecraftServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var listener = new ConnectionListener(new IPEndPoint(IPAddress.Any, 25565));
            listener.Connection += Listener_Connection;

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

        private static void Listener_Connection(UserToken connection)
        {
            Console.WriteLine("Connection: " + connection.Socket.RemoteEndPoint);
            //Task.Run(() => PlayerConnectionLoop(connection));
        }

        /*
        private static async ValueTask PlayerConnectionLoop(UserToken connection)
        {
            try
            {
                var args = connection.Awaitable.EventArgs;
                while (true)
                {
                    int bytesRead = await connection.ReceiveAsync();
                    if (bytesRead <= 0)
                        break;

                    //string request = Encoding.UTF8.GetString(args.MemoryBuffer.Span.Slice(0, bytesRead));
                    //Console.WriteLine(bytesRead + ": " + request);

                    //var output = new MemoryStream();
                    //var writer = new StreamWriter(output);
                    //writer.WriteLine("HTTP/1.0 200 OK");
                    //writer.WriteLine("Content-Type: text/html");
                    //writer.WriteLine("Connection: close");
                    //writer.WriteLine();
                    //
                    //writer.Write("well hello there");
                    //
                    //writer.Flush();
                    //connection.Socket.Send(output.GetBuffer().AsSpan(0, (int)output.Length));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(nameof(PlayerConnectionLoop) + ": " + ex.Message);
            }
            finally
            {
                // do stuff on exit :P

                connection.Close();
            }
        }
        */
    }
}
