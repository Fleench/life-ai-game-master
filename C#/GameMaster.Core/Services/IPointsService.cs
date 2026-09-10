using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IPointsService {
    Task<IEnumerable<CurrencyPool>> GetBalancesAsync();
    Task<CurrencyPool?> GetBalanceAsync(string currencyId);
    Task AwardPointsAsync(string currencyId, int amount);
    Task SpendPointsAsync(string currencyId, int amount);
}
