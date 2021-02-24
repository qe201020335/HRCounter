using System;

namespace HRCounter.Data
{
    class BPM
    {
        public string Bpm { get; set; }
        public string Measured_at { get; set; }

        
        public override string ToString()
        {
            return String.Format("BPM: {0}, measured at {1}", Bpm, Measured_at);
        }
    }
}
