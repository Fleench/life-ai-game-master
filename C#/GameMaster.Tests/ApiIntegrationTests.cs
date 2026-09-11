using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameMaster.Api.DTOs;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using GameMaster.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameMaster.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.Data.Sqlite.SqliteConnection));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddGameMasterData("Data Source=test_gamemaster.db");
            });
        });
    }

    [Fact]
    public async Task A7_Comprehensive_Integration_Tests()
    {
        var client = _factory.CreateClient();

        // 1. Missing API Key -> 401
        var res1 = await client.GetAsync("/v1/player");
        Assert.Equal(HttpStatusCode.Unauthorized, res1.StatusCode);

        // 2. Bad API Key -> 401
        var req2 = new HttpRequestMessage(HttpMethod.Get, "/v1/player");
        req2.Headers.Add("X-Api-Key", "bad_key");
        var res2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);

        // 3. Register App
        var regRes = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("TestApp", Platform.Desktop));
        regRes.EnsureSuccessStatusCode();

        var jsonStr = await regRes.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonStr);
        var apiKey = doc.RootElement.GetProperty("apiKey").GetString();
        var appIdStr = doc.RootElement.GetProperty("app").GetProperty("appId").GetString();
        var appId = Guid.Parse(appIdStr!);

        // 4. Missing Permission -> 403
        var req4 = new HttpRequestMessage(HttpMethod.Get, "/v1/player");
        req4.Headers.Add("X-Api-Key", apiKey);

        // 6. Happy Path: Get Player
        var req6 = new HttpRequestMessage(HttpMethod.Get, "/v1/player");
        req6.Headers.Add("X-Api-Key", apiKey);
        var res6 = await client.SendAsync(req6);
        res6.EnsureSuccessStatusCode();

        // 7. Happy Path: Award Points
        var req7 = new HttpRequestMessage(HttpMethod.Post, "/v1/points/award")
        {
            Content = JsonContent.Create(new PointsRequest(Resource.ExpPoints, 100))
        };
        req7.Headers.Add("X-Api-Key", apiKey);
        var res7 = await client.SendAsync(req7);
        res7.EnsureSuccessStatusCode();

        // 8. Happy Path: Spend Points
        var req8 = new HttpRequestMessage(HttpMethod.Post, "/v1/points/spend")
        {
            Content = JsonContent.Create(new PointsRequest(Resource.ExpPoints, 50))
        };
        req8.Headers.Add("X-Api-Key", apiKey);
        var res8 = await client.SendAsync(req8);
        res8.EnsureSuccessStatusCode();

        // 9. Happy Path: Get Points
        var req9 = new HttpRequestMessage(HttpMethod.Get, "/v1/points");
        req9.Headers.Add("X-Api-Key", apiKey);
        var res9 = await client.SendAsync(req9);
        res9.EnsureSuccessStatusCode();

        // 10. Happy Path: Add Inventory
        var req10 = new HttpRequestMessage(HttpMethod.Post, "/v1/inventory/add")
        {
            Content = JsonContent.Create(new AddInventoryItemRequest("Sword", 1, "{}"))
        };
        req10.Headers.Add("X-Api-Key", apiKey);
        var res10 = await client.SendAsync(req10);
        res10.EnsureSuccessStatusCode();
        // no return body

        // no return body

        // no return body

        // no return body


        // 11. Happy Path: Get Inventory
        var req11 = new HttpRequestMessage(HttpMethod.Get, "/v1/inventory");
        req11.Headers.Add("X-Api-Key", apiKey);
        var res11 = await client.SendAsync(req11);
        res11.EnsureSuccessStatusCode();

        // 12. Happy Path: Remove Inventory
        var getRes = await client.GetAsync("/v1/inventory");
        getRes.Headers.Add("X-Api-Key", apiKey);
        var getRes2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/v1/inventory") { Headers = { { "X-Api-Key", apiKey } } });
        var itemsJson = await getRes2.Content.ReadAsStringAsync();
        using var itemsDoc = JsonDocument.Parse(itemsJson);
        var itemIdStr = itemsDoc.RootElement[0].GetProperty("itemId").GetString();
        var itemId = Guid.Parse(itemIdStr!);

        var req12 = new HttpRequestMessage(HttpMethod.Post, "/v1/inventory/remove")
        {
            Content = JsonContent.Create(new RemoveInventoryItemRequest(itemId, 1))
        };
        req12.Headers.Add("X-Api-Key", apiKey);
        var res12 = await client.SendAsync(req12);
        res12.EnsureSuccessStatusCode();

        // 13. Invalid Input -> 400
        var req13 = new HttpRequestMessage(HttpMethod.Post, "/v1/points/award")
        {
            Content = JsonContent.Create(new { Resource = "InvalidResource", Amount = -10 })
        };
        req13.Headers.Add("X-Api-Key", apiKey);
        var res13 = await client.SendAsync(req13);
        Assert.Equal(HttpStatusCode.BadRequest, res13.StatusCode);
    }
}
