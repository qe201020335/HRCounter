using System;

namespace HRCounter.Data;

public interface IHRDataSource
{
    event EventHandler<HRDataReceivedEventArgs>? OnHRDataReceived;
}
