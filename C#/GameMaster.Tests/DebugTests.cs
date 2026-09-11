using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GameMaster.Api.DTOs;
using GameMaster.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Services;

namespace GameMaster.Tests;

public class DebugTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public DebugTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Check500()
    {
        var client = _factory.CreateClient();
        var regRes = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("TestApp", Platform.Desktop));
        var jsonStr = await regRes.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonStr);
        var apiKey = doc.RootElement.GetProperty("apiKey").GetString();
        var appId = Guid.Parse(doc.RootElement.GetProperty("app").GetProperty("appId").GetString()!);

        using (var scope = _factory.Services.CreateScope())
        {

        }

        var req10 = new HttpRequestMessage(HttpMethod.Post, "/v1/inventory/add")
        {
            Content = JsonContent.Create(new AddInventoryItemRequest("Sword", 1, null))
        };
        req10.Headers.Add("X-Api-Key", apiKey);
        var res10 = await client.SendAsync(req10);
        var str = await res10.Content.ReadAsStringAsync();
        _output.WriteLine(str);
    }
}
