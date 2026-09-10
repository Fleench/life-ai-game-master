using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IPlayerService {
    Task<Player?> GetPlayerAsync();
    Task UpdatePlayerAsync(Player player);
}
