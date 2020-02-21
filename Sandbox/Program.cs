using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Sandbox
{
    class Program
    {
        class what
        {
            public int order;
        }

        static void Main(string[] args)
        {
            var xd = new List<what>();

            var rand = new Random();
            for (int i = 0; i < 64; i++)
            {
                xd.Add(new what { order = rand.Next(64) });
            }

            var watch = new Stopwatch();

            static int comp(what x, what y)
            {
                return x.order.CompareTo(y.order);
            }

            for (int i = 0; i < 100 * 10_000; i++)
            {
                for (int j= 0; j < 64; j++)
                    xd[j].order = rand.Next(64);
                
                watch.Start();
                xd.Sort(comp);
                watch.Stop();
            }

            Console.WriteLine(watch.Elapsed.TotalMilliseconds + "ms total = " + watch.Elapsed.TotalMilliseconds / 1_000_000);
        }
    }
}
