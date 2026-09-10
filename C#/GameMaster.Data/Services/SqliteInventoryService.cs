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
