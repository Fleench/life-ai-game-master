#!/bin/bash
mkdir -p GameMaster.Data/Migrations GameMaster.Data/Services

# Migrations
cat << 'C_EOF' > GameMaster.Data/Migrations/001_initial_schema.sql
CREATE TABLE IF NOT EXISTS players (
    id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS currency_pools (
    currency_id TEXT PRIMARY KEY,
    balance INTEGER NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS inventory_items (
    item_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    quantity INTEGER NOT NULL,
    metadata TEXT NOT NULL,
    awarded_by_app TEXT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS connected_apps (
    app_id TEXT PRIMARY KEY,
    app_name TEXT NOT NULL,
    platform INTEGER NOT NULL,
    api_key_hash TEXT NOT NULL,
    android_uid INTEGER,
    registered_at DATETIME NOT NULL,
    last_seen_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS app_permissions (
    app_id TEXT NOT NULL,
    resource INTEGER NOT NULL,
    action INTEGER NOT NULL,
    granted INTEGER NOT NULL,
    updated_at DATETIME NOT NULL,
    PRIMARY KEY (app_id, resource, action),
    FOREIGN KEY(app_id) REFERENCES connected_apps(app_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS paired_devices (
    device_id TEXT PRIMARY KEY,
    device_name TEXT NOT NULL,
    platform INTEGER NOT NULL,
    pair_code_hash TEXT NOT NULL,
    last_sync_at DATETIME NOT NULL,
    sync_vector_clock TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sync_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    conflict_details TEXT NOT NULL,
    logged_at DATETIME NOT NULL
);
C_EOF

# Connection Factory Interface and Implementation
cat << 'C_EOF' > GameMaster.Data/IDbConnectionFactory.cs
using System.Data;
using System.Threading.Tasks;

namespace GameMaster.Data;
public interface IDbConnectionFactory {
    Task<IDbConnection> CreateConnectionAsync();
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/SqliteConnectionFactory.cs
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace GameMaster.Data;
public class SqliteConnectionFactory : IDbConnectionFactory {
    private readonly string _connectionString;
    public SqliteConnectionFactory(string connectionString) {
        _connectionString = connectionString;
    }
    public async Task<IDbConnection> CreateConnectionAsync() {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
C_EOF

# Database Initializer
cat << 'C_EOF' > GameMaster.Data/DatabaseInitializer.cs
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace GameMaster.Data;
public class DatabaseInitializer {
    private readonly IDbConnectionFactory _connectionFactory;
    public DatabaseInitializer(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory;
    }
    public async Task InitializeAsync() {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        
        // Read migration file 
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "GameMaster.Data.Migrations.001_initial_schema.sql";
        string sql = "";
        
        // For simplicity, reading from filesystem since we control it
        string basePath = AppContext.BaseDirectory;
        string path = Path.Combine(basePath, "Migrations", "001_initial_schema.sql");
        if(File.Exists(path)){
            sql = await File.ReadAllTextAsync(path);
        } else {
            // fallback if not copied
            path = "Migrations/001_initial_schema.sql";
            if(File.Exists(path)){
                sql = await File.ReadAllTextAsync(path);
            }
        }
        
        if(!string.IsNullOrEmpty(sql)) {
            await conn.ExecuteAsync(sql);
        }
    }
}
C_EOF

# Services
cat << 'C_EOF' > GameMaster.Data/Services/SqlitePlayerService.cs
using System;
using System.Threading.Tasks;
using Dapper;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqlitePlayerService : IPlayerService {
    private readonly IDbConnectionFactory _factory;
    public SqlitePlayerService(IDbConnectionFactory factory) => _factory = factory;
    
    public async Task<Player?> GetPlayerAsync() {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Player>("SELECT id as Id, display_name as DisplayName, created_at as CreatedAt, updated_at as UpdatedAt FROM players LIMIT 1");
    }
    
    public async Task UpdatePlayerAsync(Player player) {
        using var conn = await _factory.CreateConnectionAsync();
        var existing = await GetPlayerAsync();
        player.UpdatedAt = DateTime.UtcNow;
        if(existing == null) {
            player.Id = Guid.NewGuid();
            player.CreatedAt = DateTime.UtcNow;
            await conn.ExecuteAsync("INSERT INTO players (id, display_name, created_at, updated_at) VALUES (@Id, @DisplayName, @CreatedAt, @UpdatedAt)", player);
        } else {
            player.Id = existing.Id;
            await conn.ExecuteAsync("UPDATE players SET display_name = @DisplayName, updated_at = @UpdatedAt WHERE id = @Id", player);
        }
    }
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/Services/SqlitePointsService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameMaster.Core.Exceptions;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqlitePointsService : IPointsService {
    private readonly IDbConnectionFactory _factory;
    public SqlitePointsService(IDbConnectionFactory factory) => _factory = factory;
    
    public async Task<IEnumerable<CurrencyPool>> GetBalancesAsync() {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QueryAsync<CurrencyPool>("SELECT currency_id as CurrencyId, balance as Balance, updated_at as UpdatedAt FROM currency_pools");
    }
    
    public async Task<CurrencyPool?> GetBalanceAsync(string currencyId) {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<CurrencyPool>("SELECT currency_id as CurrencyId, balance as Balance, updated_at as UpdatedAt FROM currency_pools WHERE currency_id = @Id", new { Id = currencyId });
    }
    
    public async Task AwardPointsAsync(string currencyId, int amount) {
        if(amount < 0) throw new ArgumentException("Amount must be positive");
        using var conn = await _factory.CreateConnectionAsync();
        var existing = await GetBalanceAsync(currencyId);
        if(existing == null) {
            await conn.ExecuteAsync("INSERT INTO currency_pools (currency_id, balance, updated_at) VALUES (@Id, @Amount, @Now)", new { Id = currencyId, Amount = amount, Now = DateTime.UtcNow });
        } else {
            await conn.ExecuteAsync("UPDATE currency_pools SET balance = balance + @Amount, updated_at = @Now WHERE currency_id = @Id", new { Id = currencyId, Amount = amount, Now = DateTime.UtcNow });
        }
    }
    
    public async Task SpendPointsAsync(string currencyId, int amount) {
        if(amount < 0) throw new ArgumentException("Amount must be positive");
        using var conn = await _factory.CreateConnectionAsync();
        var existing = await GetBalanceAsync(currencyId);
        if(existing == null || existing.Balance < amount) throw new InsufficientPointsException($"Not enough {currencyId}");
        
        await conn.ExecuteAsync("UPDATE currency_pools SET balance = balance - @Amount, updated_at = @Now WHERE currency_id = @Id", new { Id = currencyId, Amount = amount, Now = DateTime.UtcNow });
    }
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/Services/SqliteInventoryService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqliteInventoryService : IInventoryService {
    private readonly IDbConnectionFactory _factory;
    public SqliteInventoryService(IDbConnectionFactory factory) => _factory = factory;
    
    public async Task<IEnumerable<InventoryItem>> GetItemsAsync() {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QueryAsync<InventoryItem>("SELECT item_id as ItemId, name as Name, quantity as Quantity, metadata as Metadata, awarded_by_app as AwardedByApp, created_at as CreatedAt, updated_at as UpdatedAt FROM inventory_items");
    }
    
    public async Task AddItemAsync(string name, int quantity, string metadata, string awardedByApp) {
        using var conn = await _factory.CreateConnectionAsync();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync("INSERT INTO inventory_items (item_id, name, quantity, metadata, awarded_by_app, created_at, updated_at) VALUES (@Id, @Name, @Qty, @Meta, @App, @Now, @Now)", new { Id = id, Name = name, Qty = quantity, Meta = metadata, App = awardedByApp, Now = now });
    }
    
    public async Task RemoveItemAsync(Guid itemId, int quantity) {
        using var conn = await _factory.CreateConnectionAsync();
        var item = await conn.QuerySingleOrDefaultAsync<InventoryItem>("SELECT item_id as ItemId, quantity as Quantity FROM inventory_items WHERE item_id = @Id", new { Id = itemId });
        if(item != null) {
            if(item.Quantity <= quantity) {
                await conn.ExecuteAsync("DELETE FROM inventory_items WHERE item_id = @Id", new { Id = itemId });
            } else {
                await conn.ExecuteAsync("UPDATE inventory_items SET quantity = quantity - @Qty, updated_at = @Now WHERE item_id = @Id", new { Id = itemId, Qty = quantity, Now = DateTime.UtcNow });
            }
        }
    }
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/Services/SqliteAppRegistryService.cs
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqliteAppRegistryService : IAppRegistryService {
    private readonly IDbConnectionFactory _factory;
    public SqliteAppRegistryService(IDbConnectionFactory factory) => _factory = factory;
    
    private string GenerateApiKey() {
        var key = new byte[32];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(key);
        return Convert.ToBase64String(key);
    }
    
    private string HashKey(string key) {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(bytes);
    }
    
    public async Task<(ConnectedApp App, string PlainTextKey)> RegisterAppAsync(string appName, Platform platform, int? androidUid = null) {
        using var conn = await _factory.CreateConnectionAsync();
        var plainKey = GenerateApiKey();
        var hash = HashKey(plainKey);
        
        var app = new ConnectedApp {
            AppId = Guid.NewGuid(),
            AppName = appName,
            Platform = platform,
            ApiKeyHash = hash,
            AndroidUid = androidUid,
            RegisteredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        
        await conn.ExecuteAsync("INSERT INTO connected_apps (app_id, app_name, platform, api_key_hash, android_uid, registered_at, last_seen_at) VALUES (@AppId, @AppName, @Platform, @ApiKeyHash, @AndroidUid, @RegisteredAt, @LastSeenAt)", app);
        
        return (app, plainKey);
    }
    
    public async Task<IEnumerable<ConnectedApp>> ListAppsAsync() {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QueryAsync<ConnectedApp>("SELECT app_id as AppId, app_name as AppName, platform as Platform, api_key_hash as ApiKeyHash, android_uid as AndroidUid, registered_at as RegisteredAt, last_seen_at as LastSeenAt FROM connected_apps");
    }
    
    public async Task DeregisterAppAsync(Guid appId) {
        using var conn = await _factory.CreateConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM connected_apps WHERE app_id = @Id", new { Id = appId });
    }
    
    public async Task UpdateLastSeenAsync(Guid appId) {
        using var conn = await _factory.CreateConnectionAsync();
        await conn.ExecuteAsync("UPDATE connected_apps SET last_seen_at = @Now WHERE app_id = @Id", new { Id = appId, Now = DateTime.UtcNow });
    }
    
    public async Task<ConnectedApp?> GetAppByApiKeyHashAsync(string apiKeyHash) {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<ConnectedApp>("SELECT app_id as AppId, app_name as AppName, platform as Platform, api_key_hash as ApiKeyHash, android_uid as AndroidUid, registered_at as RegisteredAt, last_seen_at as LastSeenAt FROM connected_apps WHERE api_key_hash = @Hash", new { Hash = apiKeyHash });
    }
    
    public async Task<ConnectedApp?> GetAppByAndroidUidAsync(int uid) {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<ConnectedApp>("SELECT app_id as AppId, app_name as AppName, platform as Platform, api_key_hash as ApiKeyHash, android_uid as AndroidUid, registered_at as RegisteredAt, last_seen_at as LastSeenAt FROM connected_apps WHERE android_uid = @Uid", new { Uid = uid });
    }
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/Services/SqlitePermissionsService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqlitePermissionsService : IPermissionsService {
    private readonly IDbConnectionFactory _factory;
    public SqlitePermissionsService(IDbConnectionFactory factory) => _factory = factory;
    
    public async Task GrantAsync(Guid appId, Resource resource, PermissionAction action) {
        using var conn = await _factory.CreateConnectionAsync();
        var now = DateTime.UtcNow;
        var existing = await conn.QuerySingleOrDefaultAsync<int>("SELECT COUNT(*) FROM app_permissions WHERE app_id = @AppId AND resource = @Resource AND action = @Action", new { AppId = appId, Resource = (int)resource, Action = (int)action });
        if(existing > 0) {
            await conn.ExecuteAsync("UPDATE app_permissions SET granted = 1, updated_at = @Now WHERE app_id = @AppId AND resource = @Resource AND action = @Action", new { AppId = appId, Resource = (int)resource, Action = (int)action, Now = now });
        } else {
            await conn.ExecuteAsync("INSERT INTO app_permissions (app_id, resource, action, granted, updated_at) VALUES (@AppId, @Resource, @Action, 1, @Now)", new { AppId = appId, Resource = (int)resource, Action = (int)action, Now = now });
        }
    }
    
    public async Task RevokeAsync(Guid appId, Resource resource, PermissionAction action) {
        using var conn = await _factory.CreateConnectionAsync();
        await conn.ExecuteAsync("UPDATE app_permissions SET granted = 0, updated_at = @Now WHERE app_id = @AppId AND resource = @Resource AND action = @Action", new { AppId = appId, Resource = (int)resource, Action = (int)action, Now = DateTime.UtcNow });
    }
    
    public async Task<bool> CheckAsync(Guid appId, Resource resource, PermissionAction action) {
        using var conn = await _factory.CreateConnectionAsync();
        var result = await conn.QuerySingleOrDefaultAsync<int>("SELECT granted FROM app_permissions WHERE app_id = @AppId AND resource = @Resource AND action = @Action", new { AppId = appId, Resource = (int)resource, Action = (int)action });
        return result == 1;
    }
    
    public async Task<IEnumerable<AppPermission>> GetPermissionsAsync(Guid appId) {
        using var conn = await _factory.CreateConnectionAsync();
        return await conn.QueryAsync<AppPermission>("SELECT app_id as AppId, resource as Resource, action as Action, granted as Granted, updated_at as UpdatedAt FROM app_permissions WHERE app_id = @AppId", new { AppId = appId });
    }
}
C_EOF

cat << 'C_EOF' > GameMaster.Data/Services/SqliteSyncService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
using GameMaster.Core.Services;

namespace GameMaster.Data.Services;
public class SqliteSyncService : ISyncService {
    // Stub implementation for now as full sync is Section D
    public Task<string> GeneratePairingCodeAsync() {
        return Task.FromResult("123456");
    }
    public Task ConfirmPairAsync(string code, Guid remoteDeviceId, string remotePublicKey) {
        return Task.CompletedTask;
    }
    public Task UnpairAsync(Guid deviceId) {
        return Task.CompletedTask;
    }
    public Task TriggerSyncAsync(Guid deviceId) {
        return Task.CompletedTask;
    }
    public Task<IEnumerable<PairedDevice>> GetSyncStatusAsync() {
        return Task.FromResult<IEnumerable<PairedDevice>>(new List<PairedDevice>());
    }
    public Task PushStateAsync(string delta) {
        return Task.CompletedTask;
    }
    public Task<string> GetStateSnapshotAsync() {
        return Task.FromResult("{}");
    }
}
C_EOF

# Service Collection Extensions
cat << 'C_EOF' > GameMaster.Data/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Services;
using GameMaster.Data.Services;

namespace GameMaster.Data;
public static class ServiceCollectionExtensions {
    public static IServiceCollection AddGameMasterData(this IServiceCollection services, string connectionString) {
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddTransient<DatabaseInitializer>();
        services.AddScoped<IPlayerService, SqlitePlayerService>();
        services.AddScoped<IPointsService, SqlitePointsService>();
        services.AddScoped<IInventoryService, SqliteInventoryService>();
        services.AddScoped<IAppRegistryService, SqliteAppRegistryService>();
        services.AddScoped<IPermissionsService, SqlitePermissionsService>();
        services.AddScoped<ISyncService, SqliteSyncService>();
        return services;
    }
}
C_EOF
