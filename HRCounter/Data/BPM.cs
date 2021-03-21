using System;

namespace HRCounter.Data
{
    class BPM
    {
        public int Bpm { get; set; }
        public string MeasuredAt { get; set; }

        
        public override string ToString()
        {
            return String.Format("BPM: {0}, measured at {1}", Bpm, MeasuredAt);
        }
    }
}
