using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using HRCounter.Integrations.Pulsoid.Models;
using Newtonsoft.Json;

namespace HRCounter.Integrations.Pulsoid;

internal class PulsoidApiClient : IDisposable
{
    private const string API_BASE_URL = "https://pulsoid.net/api/v1/";
    private readonly HttpClient _httpClient = new();

    public PulsoidApiClient()
    {
        _httpClient.BaseAddress = new Uri(API_BASE_URL);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Plugin.Instance.UserAgent);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<(ValidateTokenResponse? Validation, TokenErrorResponse? Error)> ValidateTokenAsync(string token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "token/validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (JsonConvert.DeserializeObject<ValidateTokenResponse>(json), null);
        }

        return (null, JsonConvert.DeserializeObject<TokenErrorResponse>(json));
    }
}
