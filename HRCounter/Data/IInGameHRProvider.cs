using System;

namespace HRCounter.Data;

internal interface IInGameHRProvider
{
    bool IsReplayData { get; }
    event Action<int>? HRChanged;
    int CurrentHR { get; }
}
