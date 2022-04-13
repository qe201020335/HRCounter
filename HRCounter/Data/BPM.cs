using HRCounter.Configuration;

namespace HRCounter.Data
{
    class BPM
    {
        public static BPM Instance = new BPM();

        private int _bpm;
        public int Bpm
        { 
            get => _bpm;
            internal set
            {
                _bpm = value;
                if (PluginConfig.Instance.AutoPause && value >= PluginConfig.Instance.PauseHR)
                {
                    Logger.logger.Info("Heart Rate too high! Pausing!");
                    Utils.GamePauseController.PauseGame();
                }
            }
        }
        public string ReceivedAt { get; internal set; }

        
        public override string ToString()
        {
            return $"BPM: {Bpm}, measured at {ReceivedAt}";
        }
    }
}
