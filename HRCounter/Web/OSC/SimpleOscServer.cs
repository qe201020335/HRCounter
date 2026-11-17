using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Utils;
using HRCounter.Web.OSC.Handlers;
using IPA.Logging;
using IPA.Utilities.Async;
using Zenject;

namespace HRCounter.Web.OSC;

internal class SimpleOscServer : IInitializable, IDisposable
{
    private const int RECEIVE_BUFFER_SIZE = 65535; // 64KB is larger than UDP datagram's maximum size

    private readonly PluginConfig _config;
    private readonly Logger _logger;
    private readonly IReadOnlyDictionary<string, IOSCMessageHandler> _handlers;

    private Socket? _socket;

    public event Action? StatusChanged;

    public bool IsListening => _socket != null;
    internal IPEndPoint? EndPoint => (IPEndPoint?)_socket?.LocalEndPoint;
    public string? ErrorMessage { get; private set; }

    public SimpleOscServer(PluginConfig config, Logger logger, IOSCMessageHandler[] handlers)
    {
        _config = config;
        _logger = logger;

        var handlersDict = new Dictionary<string, IOSCMessageHandler>();
        foreach (var handler in handlers)
        {
            foreach (var address in handler.Address)
            {
                if (!handlersDict.TryAdd(address, handler))
                {
                    _logger.Error($"Duplicate OSC handler for address '{address}'");
                    throw new InvalidOperationException($"Duplicate OSC handler for address '{address}'");
                }
            }
        }


        _handlers = handlersDict;
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
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(PluginConfig.EnableOscServer))
        {
            UpdateListener();
        }
    }

    private void UpdateListener()
    {
        if (_config.EnableOscServer && !IsListening)
        {
            StartListener();
        }
        else if (!_config.EnableOscServer && IsListening)
        {
            StopAndDisposeListener();
        }
    }

    private void StartListener()
    {
        _logger.Info("Starting OSC Server.");
        try
        {
            var endPoint = new IPEndPoint(_config.OscBindIP, _config.OscPort);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket = socket;
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
            if (!OSCHelper.TryReadString(ref data, out var addressBytes))
            {
                _logger.Warn("Failed to read OSC method address from message.");
                return;
            }

            if (data.IsEmpty || data[0] != ',' || !OSCHelper.TryReadString(ref data, out var arguments))
            {
                _logger.Warn("Failed to read OSC arguments from message.");
                return;
            }

            arguments = arguments[1..]; // skip the leading comma
            var address = Encoding.ASCII.GetString(addressBytes); // TODO compare address by bytes instead of creating a string
            _logger.Spam($"Received OSC message at '{address}' with arguments '{Encoding.ASCII.GetString(arguments)}'");

            if (!_handlers.TryGetValue(address, out var handler))
            {
                return;
            }

            handler.HandleMessage(arguments, data);
        }
        catch (Exception e)
        {
            _logger.Error("Failed to handle OSC message.");
            _logger.Error(e);
        }
    }
}
