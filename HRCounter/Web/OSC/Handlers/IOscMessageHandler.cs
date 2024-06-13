using System.Collections.Generic;

namespace HRCounter.Web.OSC.Handlers;

public interface IOSCMessageHandler
{
    string Address { get; }
    
    void HandleMessage(char[] arguments, byte[] data, int offset);
}