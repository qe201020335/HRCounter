using System;
using System.Collections.Generic;

namespace HRCounter.Web.OSC.Handlers;

public interface IOSCMessageHandler
{
    IReadOnlyList<string> Address { get; }

    void HandleMessage(ReadOnlySpan<byte> arguments, ReadOnlySpan<byte> data);
}
