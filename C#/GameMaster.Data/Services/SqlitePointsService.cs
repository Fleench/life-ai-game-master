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
