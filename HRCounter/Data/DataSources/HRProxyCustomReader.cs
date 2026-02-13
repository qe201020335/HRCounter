namespace HRCounter.Data.DataSources;

internal class HRProxyCustomReader : HRProxyBase
{
    protected override string ReaderName => "HRProxy";
    protected override string ConfigName => nameof(Config.HRProxyID);
    protected override string EventIdentifier => Config.HRProxyID;
}
