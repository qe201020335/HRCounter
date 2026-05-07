using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Integrations.Pulsoid.Models;
using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid;

internal class PulsoidOAuthClient : IDisposable
{
    private const string OAUTH_BASE_URL = "https://pulsoid.net/oauth2/";
    private const string SCOPE = "data:heart_rate:read";

    private readonly HttpClient _httpClient = new();
    private readonly string _clientId;

    public PulsoidOAuthClient(string clientId)
    {
        _clientId = clientId;
        _httpClient.BaseAddress = new Uri(OAUTH_BASE_URL);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Plugin.Instance.UserAgent);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<(StartDeviceAuthorizationResponse? Response, StartDeviceAuthorizationErrorResponse? Error)> StartDeviceAuthorization(
        CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("scope", SCOPE)
        ]);

        var response = await _httpClient.PostAsync("device_authorization", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (JsonConvert.DeserializeObject<StartDeviceAuthorizationResponse>(json), null);
        }

        return (null, JsonConvert.DeserializeObject<StartDeviceAuthorizationErrorResponse>(json));
    }

    public async Task<(ObtainTokenResponse? Token, ObtainTokenErrorResponse? Error)> TryObtainAccessToken(string deviceCode,
        CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
            new KeyValuePair<string, string>("device_code", deviceCode),
            new KeyValuePair<string, string>("client_id", _clientId)
        ]);

        var response = await _httpClient.PostAsync("token", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (JsonConvert.DeserializeObject<ObtainTokenResponse>(json), null);
        }

        return (null, JsonConvert.DeserializeObject<ObtainTokenErrorResponse>(json));
    }

    public async Task RevokeAccessToken(string token, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("token", token)
        ]);

        var response = await _httpClient.PostAsync("revoke", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
