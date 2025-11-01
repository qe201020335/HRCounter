using System;

namespace HRCounter.Data;

public interface IInGameHRProvider
{
    bool IsReplayData { get; }
    event Action<int> HRChanged;
    int GetCurrentHR();
}
