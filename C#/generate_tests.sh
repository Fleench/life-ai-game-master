#!/bin/bash
cd 'C#'
dotnet add GameMaster.Tests/GameMaster.Tests.csproj package Microsoft.Data.Sqlite
dotnet add GameMaster.Tests/GameMaster.Tests.csproj package Dapper
dotnet add GameMaster.Tests/GameMaster.Tests.csproj package Microsoft.Extensions.DependencyInjection

mkdir -p GameMaster.Tests/Services

cat << 'C_EOF' > GameMaster.Tests/TestDatabaseFixture.cs
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Data;
using GameMaster.Core.Services;
using GameMaster.Data.Services;

namespace GameMaster.Tests;
public class TestDatabaseFixture : IDisposable {
    public SqliteConnection Connection { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }
    
    public TestDatabaseFixture() {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        
        var services = new ServiceCollection();
        // Since we need to reuse the same in-memory connection, we can create a custom factory
        services.AddSingleton<IDbConnectionFactory>(new SingleConnectionFactory(Connection));
        services.AddTransient<DatabaseInitializer>();
        services.AddScoped<IPlayerService, SqlitePlayerService>();
        services.AddScoped<IPointsService, SqlitePointsService>();
        services.AddScoped<IInventoryService, SqliteInventoryService>();
        services.AddScoped<IAppRegistryService, SqliteAppRegistryService>();
        services.AddScoped<IPermissionsService, SqlitePermissionsService>();
        services.AddScoped<ISyncService, SqliteSyncService>();
        
        ServiceProvider = services.BuildServiceProvider();
        
        var initializer = ServiceProvider.GetRequiredService<DatabaseInitializer>();
        // Mock the file read by executing the raw SQL directly
        var sql = @"
            CREATE TABLE IF NOT EXISTS players (id TEXT PRIMARY KEY, display_name TEXT NOT NULL, created_at DATETIME NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS currency_pools (currency_id TEXT PRIMARY KEY, balance INTEGER NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS inventory_items (item_id TEXT PRIMARY KEY, name TEXT NOT NULL, quantity INTEGER NOT NULL, metadata TEXT NOT NULL, awarded_by_app TEXT NOT NULL, created_at DATETIME NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS connected_apps (app_id TEXT PRIMARY KEY, app_name TEXT NOT NULL, platform INTEGER NOT NULL, api_key_hash TEXT NOT NULL, android_uid INTEGER, registered_at DATETIME NOT NULL, last_seen_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS app_permissions (app_id TEXT NOT NULL, resource INTEGER NOT NULL, action INTEGER NOT NULL, granted INTEGER NOT NULL, updated_at DATETIME NOT NULL, PRIMARY KEY (app_id, resource, action));
            CREATE TABLE IF NOT EXISTS paired_devices (device_id TEXT PRIMARY KEY, device_name TEXT NOT NULL, platform INTEGER NOT NULL, pair_code_hash TEXT NOT NULL, last_sync_at DATETIME NOT NULL, sync_vector_clock TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS sync_log (id INTEGER PRIMARY KEY AUTOINCREMENT, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, conflict_details TEXT NOT NULL, logged_at DATETIME NOT NULL);
        ";
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
    
    public void Dispose() {
        Connection.Close();
        Connection.Dispose();
    }
}

public class SingleConnectionFactory : IDbConnectionFactory {
    private readonly IDbConnection _connection;
    public SingleConnectionFactory(IDbConnection connection) => _connection = connection;
    public Task<IDbConnection> CreateConnectionAsync() => Task.FromResult(_connection);
}
C_EOF

cat << 'C_EOF' > GameMaster.Tests/Services/PlayerServiceTests.cs
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
C_EOF

cat << 'C_EOF' > GameMaster.Tests/Services/PointsServiceTests.cs
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
        Assert.Equal(100, balance.Balance);
        
        await _service.SpendPointsAsync(currency, 40);
        balance = await _service.GetBalanceAsync(currency);
        Assert.Equal(60, balance.Balance);
        
        await Assert.ThrowsAsync<InsufficientPointsException>(() => _service.SpendPointsAsync(currency, 100));
    }
}
C_EOF

rm GameMaster.Tests/UnitTest1.cs
