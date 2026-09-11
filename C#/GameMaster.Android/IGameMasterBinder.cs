using GameMaster.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CoreResource = GameMaster.Core.Models.Resource;

namespace GameMaster.Android;

public interface IGameMasterBinder
{
    Task<Player?> GetPlayer();
    Task<Dictionary<CoreResource, int>> GetPoints();
    Task AwardPoints(CoreResource resource, int amount);
    Task SpendPoints(CoreResource resource, int amount);
    Task<IEnumerable<InventoryItem>> GetInventory();
    Task AddInventoryItem(string name, int qty, string? metadata);
    Task RemoveInventoryItem(Guid itemId, int qty);
}
