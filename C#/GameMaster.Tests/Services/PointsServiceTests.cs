using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Services;
using GameMaster.Core.Exceptions;
using Xunit;

namespace GameMaster.Tests.Services;
public class PointsServiceTests : IClassFixture<TestDatabaseFixture> {
    private readonly IPointsService _service;
    public PointsServiceTests(TestDatabaseFixture fixture) {
        _service = fixture.ServiceProvider.GetRequiredService<IPointsService>();
    }
    
    [Fact]
    public async Task Can_Award_And_Spend_Points() {
        var currency = Guid.NewGuid().ToString();
        await _service.AwardPointsAsync(currency, 100);
        var balance = await _service.GetBalanceAsync(currency);
        Assert.NotNull(balance);
        Assert.Equal(100, balance.Balance);
        
        await _service.SpendPointsAsync(currency, 40);
        balance = await _service.GetBalanceAsync(currency);
        Assert.Equal(60, balance.Balance);
        
        await Assert.ThrowsAsync<InsufficientPointsException>(() => _service.SpendPointsAsync(currency, 100));
    }
}
