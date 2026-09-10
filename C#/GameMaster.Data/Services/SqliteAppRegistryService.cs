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
        return BCrypt.Net.BCrypt.HashPassword(key);
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
    
    public async Task<string> RotateApiKeyAsync(Guid appId) {
        var plainKey = GenerateApiKey();
        var hash = HashKey(plainKey);
        using var conn = await _factory.CreateConnectionAsync();
        await conn.ExecuteAsync("UPDATE connected_apps SET api_key_hash = @Hash WHERE app_id = @Id", new { Hash = hash, Id = appId });
        return plainKey;
    }
}
