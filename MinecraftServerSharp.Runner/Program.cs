using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime;
using System.Threading;
using MinecraftServerSharp.Data;
using MinecraftServerSharp.NBT;
using MinecraftServerSharp.Network;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var motionBlocking = new NbtLongArray(36, "MOTION_BLOCKING");
            var mem = new MemoryStream();
            var writer = new NetBinaryWriter(mem);
            writer.Write(motionBlocking.AsCompound("Heightmaps"));
            var document = NbtDocument.Parse(mem.GetBuffer().AsMemory(0, (int)mem.Length));

            //NbtDocument document = null;
            //
            //if (false)
            //{
            //    document = NbtDocument.Parse(File.ReadAllBytes(@"C:\Users\Michal Piatkowski\Downloads\hello_world.nbt"));
            //}
            //else
            //{
            //    using (var fs = File.OpenRead(@"C:\Users\Michal Piatkowski\Downloads\bigtest.nbt"))
            //    using (var ds = new GZipStream(fs, CompressionMode.Decompress))
            //    using (var ms = new MemoryStream())
            //    {
            //        ds.SCopyTo(ms);
            //        var memory = ms.GetBuffer().AsMemory(0, (int)ms.Length);
            //
            //        //var reader = new NbtReader(memory.Span);
            //        //while (reader.Read())
            //        //{
            //        //    //Console.WriteLine(reader.NameSpan.ToUtf8String() + ": " + reader.TagType);
            //        //}
            //
            //        //for (int i = 0; i < 1_000_000; i++)
            //        {
            //            document = NbtDocument.Parse(memory);
            //            //document.Dispose();
            //            //Thread.Sleep(i % 100 == 0 ? 1 : 0);
            //        }
            //    }
            //
            //    //return;
            //
            //    Console.WriteLine();
            //    Console.WriteLine(new string('-', 20));
            //    Console.WriteLine();
            //}
            //
            var root = document.RootTag;

            Console.WriteLine(root);

            void Log(NbtElement element, int depth = 0)
            {
                string depthPad = new string(' ', depth * 3);

                foreach (var item in element.EnumerateContainer())
                {
                    Console.WriteLine(depthPad + item);

                    if (item.Type.IsContainer())
                    {
                        for (int i = 0; i < item.GetLength(); i++)
                        {
                            Console.WriteLine(depthPad + "INDEXER: " + item[i]);
                        }

                        Log(item, depth + 1);
                    }
                }
            }
            Log(root);

            var gameTicker = new GameTicker(targetTickTime: TimeSpan.FromMilliseconds(50));

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
