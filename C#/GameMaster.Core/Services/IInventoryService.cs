using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IInventoryService {
    Task<IEnumerable<InventoryItem>> GetItemsAsync();
    Task AddItemAsync(string name, int quantity, string metadata, string awardedByApp);
    Task RemoveItemAsync(Guid itemId, int quantity);
}
