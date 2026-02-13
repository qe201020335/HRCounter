using System;

namespace HRCounter.Data.DataSources;

/// <summary>
///     Legacy HypeRate data source using HRProxy.
/// </summary>
[Obsolete("Use HypeRate2 instead.", true)]
internal class HypeRate : HRProxyBase
{
    protected override string ReaderName => "hyperate";
    protected override string ConfigName => nameof(Config.HypeRateSessionID);
    protected override string EventIdentifier => Config.HypeRateSessionID;
}
