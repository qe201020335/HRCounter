using System;

namespace HRCounter.Data;

public class LiveHRProvider : IInGameHRProvider
{
    public bool IsReplayData => false;

    public event Action<int> HRChanged;

    public int GetCurrentHR() => 0;
}
