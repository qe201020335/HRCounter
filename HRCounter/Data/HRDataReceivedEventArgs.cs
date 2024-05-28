using System;

namespace HRCounter.Data
{
    public class HRDataReceivedEventArgs: EventArgs
    {
        public int HR { get; private set; }
        public string ReceivedAt { get; private set; }
        
        public HRDataReceivedEventArgs(int hr, string? receivedAt = null)
        {
            HR = hr;
            ReceivedAt = receivedAt ?? DateTime.Now.ToString("HH:mm:ss");
        }
    }
}