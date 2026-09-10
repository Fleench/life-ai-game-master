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
