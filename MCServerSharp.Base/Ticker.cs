using System;
using System.Diagnostics;
using System.Threading;

namespace MCServerSharp
{
    public class Ticker
    {
        private TimeSpan[] _elapsedTimeRing;
        private int _elapsedTimeRingIndex;

        public delegate void TickEvent(Ticker ticker);

        public event TickEvent? Tick;

        public TimeSpan TargetTime { get; }

        public bool IsRunning { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }
        public TimeSpan AverageElapsedTime { get; private set; }
        public TimeSpan TotalTime { get; private set; }
        public long TickCount { get; private set; }

        public TimeSpan SurplusTime => TargetTime - ElapsedTime;
        public TimeSpan AverageSurplusTime => TargetTime - AverageElapsedTime;

        public TimeSpan DeltaTime => ElapsedTime + FreeTime;
        public TimeSpan AverageDeltaTime => AverageElapsedTime + FreeTime;

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

        public TimeSpan AverageFreeTime
        {
            get
            {
                var surplus = AverageSurplusTime;
                if (surplus.Ticks > 0)
                    return surplus;
                return TimeSpan.Zero;
            }
        }

        public Ticker(TimeSpan targetTickTime)
        {
            TargetTime = targetTickTime;
            
            _elapsedTimeRing = new TimeSpan[Math.Max(1, (int)(1000 / TargetTime.TotalMilliseconds))];
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

                TotalTime += ElapsedTime;
                TickCount++;

                _elapsedTimeRing[_elapsedTimeRingIndex++] = ElapsedTime;
                if (_elapsedTimeRingIndex >= _elapsedTimeRing.Length)
                    _elapsedTimeRingIndex = 0;

                AverageElapsedTime = TimeSpan.Zero;
                for (int i = 0; i < _elapsedTimeRing.Length; i++)
                    AverageElapsedTime += _elapsedTimeRing[i];
                AverageElapsedTime /= _elapsedTimeRing.Length;

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
