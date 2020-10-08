using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Notch: " + MinecraftShaDigest("Notch"));
            Console.WriteLine("jeb_: " + MinecraftShaDigest("jeb_"));
            Console.WriteLine("simon: " + MinecraftShaDigest("simon"));
        }

        public static string MinecraftShaDigest(string input)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            var b = new BigInteger(hash, false, true);

            // very annoyingly, BigInteger in C# tries to be smart and puts in
            // a leading 0 when formatting as a hex number to allow roundtripping 
            // of negative numbers, thus we have to trim it off.
            if (b < 0)
            {
                string bstr = b.ToString("x");
                // toss in a negative sign if the interpreted number is negative
                return "-" + (-b).ToString("x").TrimStart('0');
            }
            else
            {
                return b.ToString("x").TrimStart('0');
            }
        }

        /*
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
        */
    }
}
