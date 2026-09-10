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
