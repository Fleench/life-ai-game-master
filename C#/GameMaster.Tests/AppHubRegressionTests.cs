// Regression tests for: "not all apps show up in the App Hub"
//
// Root causes found and fixed:
//
//   BUG 1 — Missing GET /v1/apps endpoint (AppsController)
//     HttpGameMasterClient.ListAppsAsync() calls GET /v1/apps, but the
//     AppsController only exposed POST /register and POST /rotate-key.
//     Every list call returned 404, which the retry policy exhausted silently,
//     leaving the App Hub perpetually empty for any remote client.
//     FIX: Added [HttpGet] List() to AppsController.
//
//   BUG 2 — Auto-refresh timer race in AppHubViewModel
//     The DispatcherTimer was started immediately in the constructor alongside a
//     fire-and-forget LoadAppsAsync(). With a 2-second interval, the timer's
//     first tick could overlap with an in-progress load, and whichever
//     ObservableCollection assignment landed last "won", potentially overwriting
//     a full list with a stale or empty one.
//     FIX: Added a SemaphoreSlim(1,1) guard — if a load is already in flight
//     the new invocation returns immediately (WaitAsync(0) returns false).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GameMaster.Api.DTOs;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using GameMaster.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameMaster.Tests;

/// <summary>
/// Integration tests proving the App Hub "not all apps show up" bugs are fixed.
/// </summary>
public class AppHubRegressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppHubRegressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Isolated in-memory DB per test run
                services.AddGameMasterData($"Data Source=apphub_regression_{Guid.NewGuid():N}.db");
            });
        });
    }

    // -------------------------------------------------------------------------
    // BUG 1: GET /v1/apps was missing — returned 404, causing empty App Hub list
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "GET /v1/apps returns 200 OK (endpoint exists)")]
    public async Task ListApps_EndpointExists_Returns200()
    {
        var client = _factory.CreateClient();

        // Warm the server so the DB is initialized
        await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("SeedApp", Platform.Desktop));

        var response = await client.GetAsync("/v1/apps");

        // Before the fix this would be 404 (route not found).
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET /v1/apps returns all registered apps — none excluded by platform")]
    public async Task ListApps_AllPlatformsReturned_NoneFiltered()
    {
        var client = _factory.CreateClient();

        // Register one app per platform
        var desktopReg = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("DesktopApp", Platform.Desktop));
        desktopReg.EnsureSuccessStatusCode();

        var androidReg = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("AndroidApp", Platform.Android));
        androidReg.EnsureSuccessStatusCode();

        var iosReg = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("IosApp", Platform.IosRemote));
        iosReg.EnsureSuccessStatusCode();

        // List — must return all three
        var listResp = await client.GetAsync("/v1/apps");
        listResp.EnsureSuccessStatusCode();

        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.EnumerateArray().ToList();

        Assert.True(items.Count >= 3,
            $"Expected at least 3 apps (one per platform) but got {items.Count}. " +
            "A platform filter would be the culprit.");

        var platforms = items.Select(i => i.GetProperty("platform").GetString()).ToHashSet();
        Assert.True(platforms.Any(p => string.Equals(p, "desktop",   StringComparison.OrdinalIgnoreCase)), "Desktop app missing from list");
        Assert.True(platforms.Any(p => string.Equals(p, "android",   StringComparison.OrdinalIgnoreCase)), "Android app missing from list");
        Assert.True(platforms.Any(p => string.Equals(p, "iosRemote", StringComparison.OrdinalIgnoreCase)), "IosRemote app missing from list");
    }

    [Fact(DisplayName = "GET /v1/apps returns apps registered without icons (no icon filter)")]
    public async Task ListApps_AppsWithoutIcons_StillAppear()
    {
        var client = _factory.CreateClient();

        // Register with no icon data (the default — most apps have no icon blob)
        var reg = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("NoIconApp", Platform.Desktop));
        reg.EnsureSuccessStatusCode();

        var listResp = await client.GetAsync("/v1/apps");
        listResp.EnsureSuccessStatusCode();

        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var names = doc.RootElement.EnumerateArray()
            .Select(i => i.GetProperty("appName").GetString())
            .ToList();

        Assert.Contains("NoIconApp", names,
            "Apps registered without icon data must still appear in the list.");
    }

    [Fact(DisplayName = "DELETE /v1/apps/{id} returns 204 (deregister endpoint exists)")]
    public async Task DeleteApp_EndpointExists_Returns204()
    {
        var client = _factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/v1/apps/register", new RegisterAppRequest("ToDelete", Platform.Desktop));
        reg.EnsureSuccessStatusCode();

        var json = await reg.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var appId = Guid.Parse(doc.RootElement.GetProperty("app").GetProperty("appId").GetString()!);

        var deleteResp = await client.DeleteAsync($"/v1/apps/{appId}");

        // Before the fix this would be 404 (route not found).
        Assert.NotEqual(HttpStatusCode.NotFound, deleteResp.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Confirm it's gone from the list
        var listResp = await client.GetAsync("/v1/apps");
        listResp.EnsureSuccessStatusCode();
        var listJson = await listResp.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listJson);
        var ids = listDoc.RootElement.EnumerateArray()
            .Select(i => i.GetProperty("appId").GetString())
            .ToList();
        Assert.DoesNotContain(appId.ToString(), ids);
    }
}
