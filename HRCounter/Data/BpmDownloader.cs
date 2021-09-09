using IPALogger = IPA.Logging.Logger;

namespace HRCounter.Data
{
    internal abstract class BpmDownloader
    {
        protected readonly BPM Bpm = BPM.Instance;
        protected readonly IPALogger logger = Logger.logger;

        protected abstract void RefreshSettings();

        internal abstract void Start();

        internal abstract void Stop();
        
    }
}