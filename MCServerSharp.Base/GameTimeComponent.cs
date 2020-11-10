using System;
using MCServerSharp.Components;

namespace MCServerSharp
{
    public class GameTimeComponent : Component
    {
        public Ticker Ticker { get; }

        public float Target => (float)Ticker.TargetTime.TotalSeconds;
        public float Elapsed => (float)Ticker.ElapsedTime.TotalSeconds;
        public float AverageElapsed => (float)Ticker.AverageElapsedTime.TotalSeconds;
        public float Surplus => (float)Ticker.SurplusTime.TotalSeconds;
        public float AverageSurplus => (float)Ticker.AverageSurplusTime.TotalSeconds;
        public float Free => (float)Ticker.FreeTime.TotalSeconds;
        public float AverageFree => (float)Ticker.AverageFreeTime.TotalSeconds;
        public float Delta => (float)Ticker.DeltaTime.TotalSeconds;
        public float AverageDelta => (float)Ticker.AverageDeltaTime.TotalSeconds;
        public float Total => (float)Ticker.TotalTime.TotalSeconds;
        public long TickCount => Ticker.TickCount;

        public GameTimeComponent(Ticker ticker) : base(new ComponentEntity())
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        }
    }
}
