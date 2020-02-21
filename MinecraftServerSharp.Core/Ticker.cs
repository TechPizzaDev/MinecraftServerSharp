
using System;
using System.Diagnostics;
using System.Threading;

namespace MinecraftServerSharp
{
    public class Ticker
    {
        public delegate void TickEvent(Ticker ticker);

        public event TickEvent Tick;

        public GameTime Time { get; private set; }

        public void Run()
        {
            int target = 50;
            var watch = new Stopwatch();
            while (true)
            {
                watch.Restart();
                Tick?.Invoke(this);
                watch.Stop();

                //Console.WriteLine("Tick Time: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3) + "/" + target + " ms");

                int sleep = (int)(target - Math.Floor(watch.Elapsed.TotalMilliseconds));
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }
        }
    }
}
