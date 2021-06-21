namespace HRCounter.Data
{
    class BPM
    {
        public static BPM Instance = new BPM(); 
        public int Bpm { get; internal set; }
        public string ReceivedAt { get; internal set; }

        
        public override string ToString()
        {
            return $"BPM: {Bpm}, measured at {ReceivedAt}";
        }
    }
}
