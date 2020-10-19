using System;
using System.Diagnostics;
using System.Threading;

namespace MCServerSharp
{
    public class Ticker
    {
        public delegate void TickEvent(Ticker ticker);

        public event TickEvent? Tick;

        public TimeSpan TargetTime { get; }

        public bool IsRunning { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }
        public TimeSpan SurplusTime => TargetTime - ElapsedTime;

        public TimeSpan FreeTime
        {
            get
            {
                var surplus = SurplusTime;
                if (surplus.Ticks > 0)
                    return surplus;
                return TimeSpan.Zero;
            }
        }

        public TimeSpan DeltaTime => ElapsedTime + FreeTime;

        public Ticker(TimeSpan targetTickTime)
        {
            TargetTime = targetTickTime;
        }

        public void Run()
        {
            if (IsRunning)
                throw new InvalidOperationException();
            IsRunning = true;

            long lastTicks = Stopwatch.GetTimestamp();
            long targetSleepTicks = 0;

            while (IsRunning)
            {
                long currentTicks = Stopwatch.GetTimestamp();
                long sleepTicks = currentTicks - lastTicks;
                Tick?.Invoke(this);
                lastTicks = Stopwatch.GetTimestamp();
                ElapsedTime = TimeSpan.FromTicks(lastTicks - currentTicks);

                // Try to sleep for as long as possible without overshooting the target time.
                long preciseSleepTime = TargetTime.Ticks - ElapsedTime.Ticks;
                long sleepOverheadTicks = sleepTicks - targetSleepTicks;
                targetSleepTicks = preciseSleepTime - sleepOverheadTicks;

                long sleepMillis = targetSleepTicks / TimeSpan.TicksPerMillisecond;
                if (sleepMillis > 0)
                    Thread.Sleep((int)sleepMillis);
            }
        }
    }
}
