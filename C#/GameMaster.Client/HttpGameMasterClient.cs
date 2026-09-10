using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GameMaster.Client.Models;
using Polly;
using Polly.Retry;

namespace GameMaster.Client;

public class HttpGameMasterClient : IGameMasterClient
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpGameMasterClient(HttpClient httpClient, string? apiKey = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg => (int)msg.StatusCode >= 500)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken))!;
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new GameMasterApiException(response.StatusCode, errorContent);
    }

    public async Task<RegisterResponse> RegisterAppAsync(RegisterAppRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync("v1/apps/register", request, _jsonOptions, cancellationToken));
        return await ReadResponseAsync<RegisterResponse>(response, cancellationToken);
    }

    public async Task<Player> GetPlayerAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync("v1/player", cancellationToken));
        return await ReadResponseAsync<Player>(response, cancellationToken);
    }

    public async Task<IEnumerable<CurrencyPool>> GetPointsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync("v1/points", cancellationToken));
        return await ReadResponseAsync<IEnumerable<CurrencyPool>>(response, cancellationToken);
    }

    public async Task<IEnumerable<CurrencyPool>> AwardPointsAsync(PointsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync("v1/points/award", request, _jsonOptions, cancellationToken));
        return await ReadResponseAsync<IEnumerable<CurrencyPool>>(response, cancellationToken);
    }

    public async Task<IEnumerable<CurrencyPool>> SpendPointsAsync(PointsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync("v1/points/spend", request, _jsonOptions, cancellationToken));
        return await ReadResponseAsync<IEnumerable<CurrencyPool>>(response, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync("v1/inventory", cancellationToken));
        return await ReadResponseAsync<IEnumerable<InventoryItem>>(response, cancellationToken);
    }

    public async Task AddInventoryItemAsync(AddInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync("v1/inventory/add", request, _jsonOptions, cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameMasterApiException(response.StatusCode, errorContent);
        }
    }

    public async Task RemoveInventoryItemAsync(RemoveInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync("v1/inventory/remove", request, _jsonOptions, cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameMasterApiException(response.StatusCode, errorContent);
        }
    }

    public async Task<IEnumerable<AppPermission>> GetMyPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync("v1/permissions", cancellationToken));
        return await ReadResponseAsync<IEnumerable<AppPermission>>(response, cancellationToken);
    }

    public async Task<IEnumerable<ConnectedApp>> ListAppsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync("v1/apps", cancellationToken));
        return await ReadResponseAsync<IEnumerable<ConnectedApp>>(response, cancellationToken);
    }

    public async Task RevokeAppAsync(Guid appId, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.DeleteAsync($"v1/apps/{appId}", cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameMasterApiException(response.StatusCode, errorContent);
        }
    }

    public async Task GrantPermissionAsync(Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
    {
        var request = new { Resource = resource, Action = action };
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync($"v1/permissions/{appId}/grant", request, _jsonOptions, cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameMasterApiException(response.StatusCode, errorContent);
        }
    }

    public async Task RevokePermissionAsync(Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
    {
        var request = new { Resource = resource, Action = action };
        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsJsonAsync($"v1/permissions/{appId}/revoke", request, _jsonOptions, cancellationToken));
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameMasterApiException(response.StatusCode, errorContent);
        }
    }

    public Task RequestPermissionAsync(GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default)
        => Task.CompletedTask; // REST API: no-op, permissions managed server-side
}

public class GameMasterApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string ErrorContent { get; }

    public GameMasterApiException(System.Net.HttpStatusCode statusCode, string errorContent)
        : base($"API Error: {(int)statusCode} - {errorContent}")
    {
        StatusCode = statusCode;
        ErrorContent = errorContent;
    }
}
