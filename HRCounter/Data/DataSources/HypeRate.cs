namespace HRCounter.Data.DataSources;

internal class HypeRate : HRProxyBase
{
    protected override string ReaderName => "hyperate";
    protected override string EventIdentifier => Config.HypeRateSessionID;
}
