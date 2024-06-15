using System;
using System.Net;
using IPA.Config.Data;
using IPA.Config.Stores;
using Newtonsoft.Json;

namespace HRCounter.Utils.Converters;

public class IPAddressValueConverter: ValueConverter<IPAddress>
{
    public override Value? ToValue(IPAddress? obj, object parent)
    {
       return obj == null ? null : new Text(obj.ToString());
    }

    public override IPAddress? FromValue(Value? value, object parent)
    {
        if (value is Text text)
        {
            return IPAddress.Parse(text.Value);
        }

        return null;
    }
}