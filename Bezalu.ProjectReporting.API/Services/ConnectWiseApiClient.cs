using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Bezalu.ProjectReporting.API.Services;

public interface IConnectWiseApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<List<T>?> GetListAsync<T>(string endpoint, CancellationToken cancellationToken = default);
}

public class ConnectWiseApiClient : IConnectWiseApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConnectWiseApiClient> _logger;

    public ConnectWiseApiClient(HttpClient httpClient, ILogger<ConnectWiseApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ConnectWise API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<List<T>?> GetListAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ConnectWise API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
}
