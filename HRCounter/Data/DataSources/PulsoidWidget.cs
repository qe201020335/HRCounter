namespace HRCounter.Data.DataSources;

internal class PulsoidWidget : HRProxyBase
{
    protected override string ReaderName => "pulsoid";
    protected override string ConfigName => nameof(Config.PulsoidWidgetID);
    protected override string EventIdentifier => Config.PulsoidWidgetID;
}
