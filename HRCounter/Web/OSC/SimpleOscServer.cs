using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Configuration;
using HRCounter.Web.OSC.Handlers;
using SiraUtil.Logging;
using Zenject;

namespace HRCounter.Web.OSC;

internal class SimpleOscServer : IInitializable, IDisposable
{
    private readonly PluginConfig _config;
    private readonly SiraLog _logger;
    private readonly object _listenerLock = new object();
    private readonly IReadOnlyDictionary<string, IOSCMessageHandler> _handlers;
    
    private UdpClient? _listener;
    private bool _isListening = false;
    
    public SimpleOscServer(PluginConfig config, SiraLog logger, IOSCMessageHandler[] handlers)
    {
        _config = config;
        _logger = logger;
        
        var handlersDict = new Dictionary<string, IOSCMessageHandler>();
        foreach (var handler in handlers)
        {
            foreach (var address in handler.Address)
            {
                if (handlersDict.ContainsKey(address))
                {
                    _logger.Error($"Duplicate OSC handler for address '{handler.Address}'");
                    throw new InvalidOperationException($"Duplicate OSC handler for address '{handler.Address}'");
                }
            
                handlersDict[address] = handler;
            }
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
        
            var endPoint = new IPEndPoint(_config.OscBindIP, _config.OscPort);
            var listener = new UdpClient(endPoint.Port, endPoint.AddressFamily)
            {
                EnableBroadcast = true,  // TODO: make it configurable?
                MulticastLoopback = false
            };
            
            _listener = listener;
            _isListening = true;

            Task.Run(() => ReceiveAndProcessMessages(listener, endPoint));
        }
    }
    
    private void StopAndDisposeListener()
    {
        lock (_listenerLock)
        {
            _logger.Debug("Stopping OSC Server.");
            _isListening = false;
            _listener?.Close();  // will make the Receive operation throw
            _listener = null;
        }
    }

    private void ReceiveAndProcessMessages(UdpClient listener, IPEndPoint endPoint)
    {
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
                break;
            }
            catch (SocketException e) when(e.SocketErrorCode == SocketError.Interrupted)
            {
                //Socket was closed during the receive operation
                break;
            }
            catch (SocketException e)
            {
                _logger.Warn($"Socket exception while receiving UDP message: {e.SocketErrorCode}");
                break;
            }
            catch (Exception e)
            {
                _logger.Critical("Failed to receive UDP message.");
                _logger.Critical(e);
            }
        }
    }

    private void ProcessMessage(byte[] message)
    {
        if (message.Length == 0 || message.Length % 4 != 0)
        {
            _logger.Warn("Received invalid OSC message. Message length is 0 or not a multiple of 4 bytes.");
            return;
        }
        
        if (message[0] == '#') return;  // We don't support bundles in this simple osc server

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