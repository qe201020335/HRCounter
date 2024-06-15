using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRCounter.Configuration;
using Zenject;

namespace HRCounter.Web.OSC.Handlers;

public class OscHRHandler: IOSCMessageHandler
{
    [Inject]
    private readonly PluginConfig _config;
    public IReadOnlyList<string> Address => _config.OscAddress;
    
    internal event EventHandler<int>? HeartRatePosted;
    
    public void HandleMessage(char[] arguments, byte[] data, int offset)
    {
        if (arguments.Length != 1 || arguments[0] != 'i')
        {
            return;
        }

        if (OSCHelper.TryReadInt32(data, ref offset, out var number))
        {
            _ = Task.Run(() =>
            {
                var e = HeartRatePosted;
                e?.Invoke(this, number);
            });
        }
        else
        {
            Console.WriteLine("Failed to parse heart rate");
        }
    }
}