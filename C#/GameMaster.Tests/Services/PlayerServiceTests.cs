using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Xunit;

namespace GameMaster.Tests.Services;
public class PlayerServiceTests : IClassFixture<TestDatabaseFixture> {
    private readonly IPlayerService _service;
    public PlayerServiceTests(TestDatabaseFixture fixture) {
        _service = fixture.ServiceProvider.GetRequiredService<IPlayerService>();
    }
    
    [Fact]
    public async Task Can_Update_And_Get_Player() {
        var p = new Player { DisplayName = "TestPlayer" };
        await _service.UpdatePlayerAsync(p);
        var result = await _service.GetPlayerAsync();
        Assert.NotNull(result);
        Assert.Equal("TestPlayer", result.DisplayName);
    }
}
