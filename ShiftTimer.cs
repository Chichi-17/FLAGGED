using System;
using System.Diagnostics;

namespace FLAGGED
{
    // =====================================================================
    //  TIME LOGIC
    //  A 3-minute countdown for each shift. It does not print or block
    //  anything: the UI just reads RemainingSeconds / Format() / IsTimeUp
    //  from its own UI timer tick.
    // =====================================================================
    public class ShiftTimer
    {
        private readonly Stopwatch _watch = new Stopwatch();
        private int _penaltySeconds;

        public void Start()
        {
            _penaltySeconds = 0;
            _watch.Reset();
            _watch.Start();
        }

        // Freezes the clock (used when the shift ends early).
        public void Stop()
        {
            _watch.Stop();
        }

        // Takes seconds off the clock (used for database lookups).
        public void AddPenalty(int seconds)
        {
            if (seconds > 0) _penaltySeconds += seconds;
        }

        private double RemainingExact =>
            Math.Max(0, GameRules.ShiftSeconds - _watch.Elapsed.TotalSeconds - _penaltySeconds);

        public int RemainingSeconds => (int)Math.Ceiling(RemainingExact);

        public bool IsTimeUp => RemainingExact <= 0;

        // "03:00", "02:59", ... "00:00"
        public string Format()
        {
            int total = RemainingSeconds;
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
