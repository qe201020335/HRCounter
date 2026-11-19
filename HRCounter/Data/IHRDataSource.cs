using System;

namespace HRCounter.Data;

public interface IHRDataSource
{
    /// <summary>
    ///     Raise when a new heart rate sample arrives. Can be raised from any thread.
    ///     The sender must be this data source instance. Events from any other sender are ignored.
    /// </summary>
    event EventHandler<int>? HRDataReceived;
    bool AllowReplayRecording { get; }
}
