using System;

namespace HRCounter.Events
{
    public class HRUpdateEventArgs : EventArgs
    {
        public readonly int HeartRate;

        internal HRUpdateEventArgs(int hr)
        {
            HeartRate = hr;
        }
    }
}