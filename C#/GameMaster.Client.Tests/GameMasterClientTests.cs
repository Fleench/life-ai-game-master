using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameMaster.Client.Models;
using RichardSzalay.MockHttp;
using Xunit;

namespace GameMaster.Client.Tests;

public class HttpGameMasterClientTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task GetPlayerAsync_ReturnsPlayer()
    {
        var mockHttp = new MockHttpMessageHandler();
        var expectedPlayer = new Player { Id = Guid.NewGuid(), DisplayName = "Test" };
        
        mockHttp.When(HttpMethod.Get, "http://localhost/v1/player")
                .Respond("application/json", JsonSerializer.Serialize(expectedPlayer, _jsonOptions));
                
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("http://localhost/") };
        var client = new HttpGameMasterClient(httpClient, "test-api-key");

        var player = await client.GetPlayerAsync();

        Assert.Equal(expectedPlayer.Id, player.Id);
        Assert.Equal("Test", player.DisplayName);
        Assert.True(httpClient.DefaultRequestHeaders.Contains("X-Api-Key"));
    }
    
    [Fact]
    public async Task Retry_On500Error_RetriesThreeTimes()
    {
        var mockHttp = new MockHttpMessageHandler();
        var requestCount = 0;
        
        mockHttp.When(HttpMethod.Get, "http://localhost/v1/player")
                .Respond(req => 
                {
                    requestCount++;
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                });
                
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("http://localhost/") };
        var client = new HttpGameMasterClient(httpClient);

        await Assert.ThrowsAsync<GameMasterApiException>(() => client.GetPlayerAsync());
        Assert.Equal(4, requestCount); // 1 initial + 3 retries
    }
    
    [Fact]
    public async Task RegisterAppAsync_ReturnsResponse()
    {
        var mockHttp = new MockHttpMessageHandler();
        var expectedResponse = new RegisterResponse 
        { 
            App = new ConnectedApp { AppId = Guid.NewGuid(), AppName = "Test" },
            ApiKey = "secret"
        };
        
        mockHttp.When(HttpMethod.Post, "http://localhost/v1/apps/register")
                .Respond("application/json", JsonSerializer.Serialize(expectedResponse, _jsonOptions));
                
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("http://localhost/") };
        var client = new HttpGameMasterClient(httpClient);

        var response = await client.RegisterAppAsync(new RegisterAppRequest("Test", Platform.Desktop));

        Assert.Equal("secret", response.ApiKey);
        Assert.Equal("Test", response.App.AppName);
    }
}
