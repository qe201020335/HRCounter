using System;

namespace HRCounter.Data.DataSources
{
    internal class PulsoidWidget: HRProxyBase
    {
        protected override string ReaderName => "pulsoid";
        protected override string EventIdentifier => Config.PulsoidWidgetID;
    }
}