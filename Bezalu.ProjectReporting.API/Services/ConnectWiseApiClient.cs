using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace Bezalu.ProjectReporting.API.Services;

public interface IConnectWiseApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<List<T>?> GetListAsync<T>(string endpoint, CancellationToken cancellationToken = default);
}

// Primary constructor (C# 12) with parameters used in initializers
public class ConnectWiseApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ConnectWiseApiClient> logger) : IConnectWiseApiClient
{
    private readonly string _baseUrl = configuration["ConnectWise:BaseUrl"] ?? "https://api-na.myconnectwise.net/v4_6_release/apis/3.0/";
    private readonly string _companyId = configuration["ConnectWise:CompanyId"] ?? "";
    private readonly string _publicKey = configuration["ConnectWise:PublicKey"] ?? "";
    private readonly string _privateKey = configuration["ConnectWise:PrivateKey"] ?? "";
    private readonly string _clientId = configuration["ConnectWise:ClientId"] ?? "";
    private readonly string _apiVersion = configuration["ConnectWise:ApiVersion"] ?? "";

    private bool _configured;

    private void EnsureConfigured()
    {
        if (_configured) return;

        if (string.IsNullOrWhiteSpace(_companyId) || string.IsNullOrWhiteSpace(_publicKey) || string.IsNullOrWhiteSpace(_privateKey) || string.IsNullOrWhiteSpace(_clientId))
        {
            logger.LogError("Missing ConnectWise credentials (CompanyId/PublicKey/PrivateKey/ClientId). Calls may fail.");
        }

        if (string.IsNullOrWhiteSpace(_apiVersion))
        {
            // default to latest known version from spec snippet
            // Do not log as error; absence is acceptable
            logger.LogDebug("ApiVersion not set; defaulting to 2025.8");
        }
        var version = string.IsNullOrWhiteSpace(_apiVersion) ? "2025.8" : _apiVersion;

        // Configure HttpClient
        httpClient.BaseAddress = new Uri(_baseUrl);

        var authString = $"{_companyId}+{_publicKey}:{_privateKey}";
        var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);

        httpClient.DefaultRequestHeaders.Remove("clientId");
        httpClient.DefaultRequestHeaders.Add("clientId", _clientId);

        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", $"application/vnd.connectwise.com+json; version={version}");

        _configured = true;
    }

    private void AddTraceParent()
    {
        var traceId = System.Diagnostics.ActivityTraceId.CreateRandom();
        var spanId = System.Diagnostics.ActivitySpanId.CreateRandom();
        httpClient.DefaultRequestHeaders.Remove("traceparent");
        httpClient.DefaultRequestHeaders.Add("traceparent", $"00-{traceId}-{spanId}-01");
    }

    private async Task ThrowDetailedExceptionAsync(HttpResponseMessage response, string endpoint)
    {
        string body;
        try { body = await response.Content.ReadAsStringAsync(); } catch { body = "<unable to read body>"; }
        logger.LogError("ConnectWise API error {Status} for {Endpoint}. Body: {Body}", (int)response.StatusCode, endpoint, body);
        throw new HttpRequestException($"ConnectWise API call failed ({(int)response.StatusCode}) for {endpoint}. Body: {body}");
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        AddTraceParent();
        logger.LogDebug("GET {Endpoint}", endpoint);
        var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowDetailedExceptionAsync(response, endpoint);
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    public async Task<List<T>?> GetListAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        AddTraceParent();
        logger.LogDebug("GET (list) {Endpoint}", endpoint);
        var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowDetailedExceptionAsync(response, endpoint);
        }
        return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: cancellationToken);
    }
}
