using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Utils;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.Web.OSC;

internal class SimpleOscServer : IInitializable, IDisposable, IOscServer
{
    private const int RECEIVE_BUFFER_SIZE = 65535; // 64KB is larger than UDP datagram's maximum size

    private readonly PluginConfig _config;
    private readonly Logger _logger;

    /// <summary>
    ///     A set of registered handlers. This must only be touched on the main thread.
    /// </summary>
    private readonly System.Collections.Generic.HashSet<IOscMessageHandler> _registeredHandlers = [];

    /// <summary>
    ///     The current address mappings for the registered handlers.
    ///     This array instance will be replaced on change, and can be read from any thread.
    /// </summary>
    private volatile HandlerEntry[] _handlers = [];

    private Socket? _socket;
    private IPEndPoint? _bindEndPoint;

    public event Action? StatusChanged;

    public bool IsListening => _socket != null;
    public IPEndPoint? EndPoint => (IPEndPoint?)_socket?.LocalEndPoint;
    public string? ErrorMessage { get; private set; }

    internal SimpleOscServer(PluginConfig config, Logger logger)
    {
        _config = config;
        _logger = logger;
    }

    public void Initialize()
    {
        UpdateListener();
        _config.PropertyChanged += OnConfigChanged;
    }

    public void Dispose()
    {
        _config.PropertyChanged -= OnConfigChanged;
        StopAndDisposeListener();
    }

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(PluginConfig.EnableOscServer) or nameof(PluginConfig.OscBindIP) or nameof(PluginConfig.OscPort))
        {
            UpdateListener();
        }
    }

    private void UpdateListener()
    {
        if (IsListening && (!_config.EnableOscServer || !Equals(_bindEndPoint?.Address, _config.OscBindIP) || _bindEndPoint?.Port != _config.OscPort))
        {
            StopAndDisposeListener();
        }

        if (_config.EnableOscServer && !IsListening)
        {
            StartListener();
        }
    }

    private void StartListener()
    {
        _logger.Info($"Starting OSC Server on {_config.OscBindIP}:{_config.OscPort}.");
        try
        {
            var endPoint = new IPEndPoint(_config.OscBindIP, _config.OscPort);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket = socket;
            _bindEndPoint = endPoint;
            socket.Bind(endPoint);
            ErrorMessage = null;

            Task.Factory.StartNew(() =>
                ReceiveAndProcessMessages(socket), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            InvokeStatusChanged();
        }
        catch (Exception e)
        {
            var reason = e is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse }
                ? "The OSC (UDP) port is already in use."
                : e.Message;
            _logger.Error($"Failed to start OSC server: {reason}");
            _logger.Error(e);
            StopAndDisposeListener(reason);
        }
    }

    private void StopAndDisposeListener(string? reason = null)
    {
        _logger.Info("Stopping OSC Server.");
        ErrorMessage = reason;
        _socket?.Close(); // makes the blocking Receive throw
        _socket = null;
        _bindEndPoint = null;
        InvokeStatusChanged();
    }

    private void InvokeStatusChanged()
    {
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            var action = StatusChanged;
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                _logger.Error("Failed to invoke OSC server status changed event.");
                _logger.Error(e);
            }
        });
    }

    private void ReceiveAndProcessMessages(Socket socket)
    {
        _logger.Debug("Start receiving OSC messages.");
        var buffer = new byte[RECEIVE_BUFFER_SIZE];
        try
        {
            while (true)
            {
                // The async versions will create a task for each message
                var size = socket.Receive(buffer);
                ProcessMessage(buffer.AsSpan(0, size));
            }
        }
        catch (Exception e)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                if (_socket != socket) return; // socket closed

                _logger.Critical("Stopped receiving OSC messages unexpectedly.");
                _logger.Critical(e);
                StopAndDisposeListener(e.Message);
            });
        }
    }

    private void ProcessMessage(ReadOnlySpan<byte> data)
    {
        try
        {
            if (data.Length == 0 || data.Length % 4 != 0)
            {
                _logger.Warn("Received invalid OSC message. Message length is 0 or not a multiple of 4 bytes.");
                return;
            }

            if (data[0] != '/') return; // We don't support bundles (#) in this simple osc server

            //b'address_,i__nnnn'
            if (!OscHelper.TryReadString(ref data, out var address))
            {
                _logger.Warn("Failed to read OSC method address from message.");
                return;
            }

            if (data.IsEmpty || data[0] != ',' || !OscHelper.TryReadString(ref data, out var arguments))
            {
                _logger.Warn("Failed to read OSC arguments from message.");
                return;
            }

            arguments = arguments[1..]; // skip the leading comma
            _logger.Spam($"Received OSC message at '{Encoding.ASCII.GetString(address)}' with arguments '{Encoding.ASCII.GetString(arguments)}'");

            if (FindHandler(address) is { } handler)
            {
                handler.HandleMessage(arguments, data);
            }
        }
        catch (Exception e)
        {
            _logger.Error("Failed to handle OSC message.");
            _logger.Error(e);
        }
    }

    private IOscMessageHandler? FindHandler(ReadOnlySpan<byte> address)
    {
        var handlers = _handlers;
        foreach (var entry in handlers)
        {
            if (address.SequenceEqual(entry.Address))
            {
                return entry.Handler;
            }
        }

        return null;
    }

    public void RegisterHandler(IOscMessageHandler handler)
    {
        ThreadGuard.EnsureOnMainThread();

        _logger.Debug($"Registering handler {handler}");
        if (!_registeredHandlers.Add(handler))
        {
            _logger.Warn($"Handler {handler} is already registered.");
            return;
        }

        UpdateHandlerEntries(handler, true);
    }

    public void UnregisterHandler(IOscMessageHandler handler)
    {
        ThreadGuard.EnsureOnMainThread();

        _logger.Debug($"Unregistering handler {handler}");
        if (!_registeredHandlers.Remove(handler))
        {
            _logger.Warn($"Handler {handler} is not registered.");
            return;
        }

        UpdateHandlerEntries(handler, false);
    }

    public void UpdateHandler(IOscMessageHandler handler)
    {
        ThreadGuard.EnsureOnMainThread();

        _logger.Debug($"Updating addresses for handler {handler}");
        if (!_registeredHandlers.Contains(handler))
        {
            _logger.Warn($"Handler {handler} is not registered.");
            return;
        }

        UpdateHandlerEntries(handler, true);
    }

    private void UpdateHandlerEntries(IOscMessageHandler handler, bool include)
    {
        var entries = _handlers.Where(entry => entry.Handler != handler).ToList();
        if (include)
        {
            foreach (var address in handler.Addresses)
            {
                if (string.IsNullOrEmpty(address) || address[0] != '/')
                {
                    _logger.Warn($"Ignoring invalid OSC address '{address}' for handler {handler}.");
                    continue;
                }

                var bytes = Encoding.ASCII.GetBytes(address);
                if (entries.Any(entry => entry.Address.AsSpan().SequenceEqual(bytes)))
                {
                    _logger.Warn($"OSC address '{address}' has already been registered, ignoring duplicated address for handler {handler}.");
                }
                else
                {
                    entries.Add(new HandlerEntry(bytes, handler));
                }
            }
        }

        _handlers = [.. entries];
    }

    private readonly struct HandlerEntry(byte[] address, IOscMessageHandler handler)
    {
        public readonly byte[] Address = address;
        public readonly IOscMessageHandler Handler = handler;
    }
}
