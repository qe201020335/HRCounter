using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Web.OSC.Handlers;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Web.OSC;

internal class SimpleOscServer : IInitializable, IDisposable
{
    private readonly PluginConfig _config;
    private readonly SiraLog _logger;
    private readonly object _listenerLock = new();
    private readonly IReadOnlyDictionary<string, IOSCMessageHandler> _handlers;

    private UdpClient? _listener;
    private IPEndPoint? _endPoint;
    private bool _isListening = false;

    public event Action? StatusChanged;

    public bool IsListening => _isListening;
    internal IPEndPoint? EndPoint => _endPoint;
    public string? ErrorMessage { get; private set; }

    public SimpleOscServer(PluginConfig config, SiraLog logger, IOSCMessageHandler[] handlers)
    {
        _config = config;
        _logger = logger;

        var handlersDict = new Dictionary<string, IOSCMessageHandler>();
        foreach (var handler in handlers)
        foreach (var address in handler.Address)
        {
            if (handlersDict.ContainsKey(address))
            {
                _logger.Error($"Duplicate OSC handler for address '{handler.Address}'");
                throw new InvalidOperationException($"Duplicate OSC handler for address '{handler.Address}'");
            }

            handlersDict[address] = handler;
        }

        _handlers = handlersDict;
    }

    public void Initialize()
    {
        UpdateListener();
        _config.OnSettingsChanged += UpdateListener;
    }

    public void Dispose()
    {
        _config.OnSettingsChanged -= UpdateListener;
        StopAndDisposeListener();
    }

    private void UpdateListener()
    {
        if (_config.EnableOscServer && !_isListening)
        {
            StartListener();
        }
        else if (!_config.EnableOscServer && _isListening)
        {
            StopAndDisposeListener();
        }
    }

    private void StartListener()
    {
        lock (_listenerLock)
        {
            _logger.Debug("Starting OSC Server.");
            if (_isListening)
            {
                _logger.Warn("OSC server is already listening.");
                return;
            }

            try
            {
                _endPoint = new IPEndPoint(_config.OscBindIP, _config.OscPort);
                var listener = new UdpClient(_endPoint)
                {
                    EnableBroadcast = true, // TODO: make it configurable?
                    MulticastLoopback = false
                };

                _listener = listener;
                _isListening = true;
                ErrorMessage = null;

                Task.Run(() => ReceiveAndProcessMessages(listener, _endPoint));
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    _logger.Error("The OSC (UDP) port is already in use.");
                    _logger.Debug(e);
                    ErrorMessage = "The OSC (UDP) port is already in use.";
                }
                else
                {
                    _logger.Critical($"SocketException while trying to create UDP Client:  ({e.SocketErrorCode})");
                    _logger.Critical(e);
                    ErrorMessage = e.SocketErrorCode + "\n" + e.Message;
                }

                _isListening = false;
                CleanUpListener();
            }
            catch (Exception e)
            {
                _logger.Error("Failed to start OSC server: " + e.Message);
                _logger.Error(e);
                _isListening = false;
                ErrorMessage = e.Message;
                CleanUpListener();
            }
            finally
            {
                InvokeStatusChanged();
            }
        }
    }

    private void StopAndDisposeListener(string? reason = null)
    {
        lock (_listenerLock)
        {
            _logger.Debug("Stopping OSC Server.");
            _isListening = false;
            ErrorMessage = reason;
            CleanUpListener();
            InvokeStatusChanged();
        }
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

    private void CleanUpListener()
    {
        _endPoint = null;
        _listener?.Close(); // will make the Receive operation throw
        _listener = null;
    }

    private void ReceiveAndProcessMessages(UdpClient listener, IPEndPoint endPoint)
    {
        Exception? exception = null;
        while (_isListening)
        {
            try
            {
                var message = listener.Receive(ref endPoint);
                _ = Task.Run(() => ProcessMessage(message));
            }
            catch (ObjectDisposedException)
            {
                _logger.Trace("UDP client was disposed.");
                return;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted)
            {
                //Socket was closed during the receive operation
                return;
            }
            catch (SocketException e)
            {
                _logger.Critical($"Socket exception while receiving UDP message: {e.SocketErrorCode}");
                exception = e;
                break;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to receive UDP message.");
                _logger.Error(e);
                exception = e;
                break;
            }
        }

        if (_isListening)
        {
            _logger.Warn("UDP Socket was exceptionally interrupted, disposing.");
            StopAndDisposeListener(exception?.Message ?? "Unknown error");
        }
    }

    private void ProcessMessage(byte[] message)
    {
        if (message.Length == 0 || message.Length % 4 != 0)
        {
            _logger.Warn("Received invalid OSC message. Message length is 0 or not a multiple of 4 bytes.");
            return;
        }

        if (message[0] == '#') return; // We don't support bundles in this simple osc server

        var offset = 0;
        var address = ReadAddress(message, ref offset);
        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.Warn("Failed to read OSC method address from message.");
            return;
        }

        //b'address_,i__nnnn'
        if (offset >= message.Length || message[offset] != ',')
        {
            _logger.Warn("Failed to read OSC arguments from message. No comma after address.");
            _logger.Trace($"Offset: {offset}, Message length: {message.Length}");
            return; // bad message
        }

        var arguments = OSCHelper.ReadString(message, ref offset)?.Skip(1).ToArray();
        if (arguments == null)
        {
            _logger.Warn("Failed to read OSC arguments from message.");
            return;
        }

        if (arguments.Length > 0 && offset >= message.Length)
        {
            _logger.Warn("OSC message with arguments but no data.");
            return;
        }

        _logger.Trace($"Received OSC message at '{address}' with arguments '{new string(arguments)}'");

        if (_handlers.TryGetValue(address!, out var handler))
        {
            try
            {
                handler.HandleMessage(arguments, message, offset);
            }
            catch (Exception e)
            {
                _logger.Critical("Failed to handle OSC message.");
                _logger.Critical(e);
            }
        }
    }

    private string? ReadAddress(byte[] message, ref int offset)
    {
        if (offset >= message.Length)
        {
            return null;
        }

        if (message[offset] != '/')
        {
            return null;
        }

        return OSCHelper.ReadString(message, ref offset);
    }
}
