using System;
using IPA.Logging;

namespace HRCounter.Utils;

internal sealed class SafeEvent<T>
{
    private static readonly Logger _logger = Plugin.GetChildLogger("SafeEvent");
    private readonly object _lock = new();
    private Action<T>[] _handlers = [];

    public void Add(Action<T>? handler)
    {
        if (handler == null) return;
        lock (_lock)
        {
            _handlers = [.. _handlers, handler];
        }
    }

    public void Remove(Action<T>? handler)
    {
        if (handler == null) return;
        lock (_lock)
        {
            var i = Array.LastIndexOf(_handlers, handler);
            if (i < 0) return;
            var copy = new Action<T>[_handlers.Length - 1];
            Array.Copy(_handlers, 0, copy, 0, i);
            Array.Copy(_handlers, i + 1, copy, i, copy.Length - i);
            _handlers = copy;
        }
    }

    public void Invoke(T arg)
    {
        var handlers = _handlers;
        foreach (var handler in handlers)
        {
            try
            {
                handler(arg);
            }
            catch (Exception e)
            {
                _logger.Critical(e);
            }
        }
    }
}
