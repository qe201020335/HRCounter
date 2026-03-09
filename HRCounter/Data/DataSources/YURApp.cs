using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Data.DataSources.Base;
using HRCounter.Utils;
using IPA.Logging;
using Newtonsoft.Json.Linq;
using Zenject;

namespace HRCounter.Data.DataSources;

internal sealed class YURApp : DataSource
{
    [Inject]
    private readonly Logger _logger = null!;

    private const string HOST = "127.0.0.1";
    private const int PORT = 11010;
    private TcpClient? _client;

    private bool _running;

    private Thread? _worker;

    private CancellationTokenSource? _cancellationSource;

    protected override void Start()
    {
        _running = true;
        if (_client != null)
        {
            _logger.Info("We have an old tcp client, destroying");
            CloseClientMakeNull();
        }

        _logger.Info("Creating new tcp connection");
        _client = new TcpClient();
        _cancellationSource?.Cancel();
        _cancellationSource = new CancellationTokenSource();
        _worker = new Thread(StartClient);
        _worker.Start();
    }

    private void StartClient()
    {
        try
        {
            _client?.ConnectAsync(HOST, PORT).Wait(_cancellationSource!.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn("Connect Canceled.");
        }
        catch (Exception e)
        {
            _logger.Warn("Cannot connect to YUR app");
            _logger.Warn(e.Message);
            _logger.Debug(e);
            Stop();
            return;
        }

        try
        {
            ReadMessage().Wait(_cancellationSource!.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn("Read Canceled.");
        }
        catch (Exception e)
        {
            _logger.Critical("Exception occured while receiving data");
            _logger.Critical(e.Message);
            _logger.Debug(e);
            Stop();
        }
    }


    private async Task ReadMessage()
    {
        while (_running)
        {
            if (_client?.Connected != true)
            {
                return;
            }

            try
            {
                // steam operations feel a lot like c...
                // first get message type
                var type = new byte[1];
                if (await _client.GetStream().ReadAsync(type, 0, 1) < 1)
                {
                    throw new Exception("Cannot read message type");
                }

                if (type[0] == 1)
                {
                    // ping message
                    _logger.Spam("Ping!");
                    await Pong();
                }
                else if (type[0] == 20)
                {
                    // we have data
                    byte[] dataBytes;
                    using (var reader = new BinaryReader(_client.GetStream(), Encoding.UTF8, true))
                    {
                        var messageSize = reader.ReadInt32();
                        dataBytes = reader.ReadBytes(messageSize);
                    }

                    HandleData(Encoding.UTF8.GetString(dataBytes));
                }
            }
            catch (ObjectDisposedException)
            {
                if (_running)
                {
                    _logger.Warn("tcp client is not connected anymore");
                }

                return;
            }
            catch (Exception e)
            {
                _logger.Critical("Exception occured while reading data");
                _logger.Critical(e.Message);
                _logger.Debug(e);
                return;
            }
        }
    }


    private Task SendMessage(byte type, string? data)
    {
        byte[] dataArray;
        if (string.IsNullOrEmpty(data))
        {
            dataArray = new[] { type };
        }
        else
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);

            var countBytes = BitConverter.GetBytes(dataBytes.Length);

            dataArray = new byte[dataBytes.Length + 5];

            countBytes.CopyTo(dataArray, 1);
            dataBytes.CopyTo(dataArray, 5);
        }

        dataArray[0] = type;

        try
        {
            using var writer = new BinaryWriter(_client?.GetStream(), Encoding.UTF8, true);
            writer.Write(dataArray);
            writer.Flush();
            return _client?.GetStream().FlushAsync() ?? Task.CompletedTask;
        }
        catch (Exception e)
        {
            _logger.Critical($"Exception trying to send data through socket: {e.Message}");
            _logger.Debug(e);
            return Task.CompletedTask;
        }
    }

    private void HandleData(string data)
    {
        _logger.Spam(data);

        try
        {
            var json = JObject.Parse(data);
            if (json["actionType"]?.ToString() != "OverlayUpdate")
            {
                return;
            }

            var jsonData = json["jsonData"]?.ToString();
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                return;
            }

            _logger.Spam(jsonData!);
            var osu = JObject.Parse(jsonData!);
            var hrToken = osu["status"]?["heartRate"]?.Type != JTokenType.Null
                ? osu["status"]?["heartRate"]
                : osu["status"]?["calculationMetrics"]?["estHeartRate"];
            if (hrToken is { Type: JTokenType.Integer })
            {
                OnHeartRateDataReceived(hrToken.ToObject<int>());
            }
        }
        catch (Exception e)
        {
            _logger.Warn("Exception occured while parsing hr data");
            _logger.Warn(e.Message);
            _logger.Debug(e);
        }
    }


    private async Task Pong()
    {
        _logger.Spam("Pong!");
        await SendMessage(2, null);
    }


    private void CloseClientMakeNull()
    {
        // close current one 
        try
        {
            _client?.Close();
        }
        catch (Exception e)
        {
            _logger.Warn("Exception occured while closing tcp client");
            _logger.Warn(e.Message);
            _logger.Debug(e);
        }

        _client = null;
    }

    protected override void Stop()
    {
        _running = false;
        _cancellationSource?.Cancel();
        _cancellationSource?.Dispose();
        _cancellationSource = null;
        CloseClientMakeNull();
        _worker = null;
        _logger.Info("Stopped");
    }
}
