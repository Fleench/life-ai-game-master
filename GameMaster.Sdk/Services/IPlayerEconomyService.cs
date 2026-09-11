using System.Threading.Tasks;
using GameMaster.Sdk.Models;

namespace GameMaster.Sdk.Services;

public interface IPlayerEconomyService
{
    /// <summary>
    /// Retrieves the current player's profile including Points and Coins.
    /// </summary>
    Task<PlayerProfile> GetPlayerProfileAsync();

    /// <summary>
    /// Adds or removes coins from the current player's balance.
    /// Use a negative amount to remove coins.
    /// </summary>
    /// <param name="amount">The number of coins to add (positive) or remove (negative).</param>
    /// <param name="reason">An optional reason for the transaction.</param>
    /// <returns>A result indicating success or failure along with the new balance.</returns>
    Task<CoinTransactionResult> AdjustCoinsAsync(int amount, string reason = "");
    
    /// <summary>
    /// Adds or removes a specific resource from the current player's balance.
    /// Use a negative amount to remove the resource.
    /// </summary>
    Task<CoinTransactionResult> AdjustResourceAsync(string resource, int amount, string reason = "");
}
