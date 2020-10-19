using System;
using MCServerSharp.Components;

namespace MCServerSharp
{
    public class GameTimeComponent : Component
    {
        public Ticker Ticker { get; }

        public float Target => (float)Ticker.TargetTime.TotalSeconds;
        public float Elapsed => (float)Ticker.ElapsedTime.TotalSeconds;
        public float Surplus => (float)Ticker.SurplusTime.TotalSeconds;
        public float Free => (float)Ticker.FreeTime.TotalSeconds;
        public float Delta => (float)Ticker.DeltaTime.TotalSeconds;

        public GameTimeComponent(Ticker ticker) : base(new ComponentEntity())
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        }
    }
}
