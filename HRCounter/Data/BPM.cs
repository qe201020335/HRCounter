using System;

namespace HRCounter.Data
{
    class BPM
    {
        public static int Bpm { get; private set; }
        public static string ReceivedAt { get; private set; } = string.Empty;

        internal static void Set(int b, string? receiveAt = null)
        {
            ReceivedAt = receiveAt ?? DateTime.Now.ToString("HH:mm:ss");
            Bpm = b;
        }

        public static string Str => $"BPM: {Bpm}, measured at {ReceivedAt}";
    }
}