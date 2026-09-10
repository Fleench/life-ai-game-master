using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameMaster.Client.Models;

namespace GameMaster.Client;

public interface IGameMasterClient
{
    Task<RegisterResponse> RegisterAppAsync(RegisterAppRequest request, CancellationToken cancellationToken = default);
    Task<Player> GetPlayerAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyPool>> GetPointsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyPool>> AwardPointsAsync(PointsRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyPool>> SpendPointsAsync(PointsRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default);
    Task AddInventoryItemAsync(AddInventoryItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveInventoryItemAsync(RemoveInventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppPermission>> GetMyPermissionsAsync(CancellationToken cancellationToken = default);

    // Missing methods for apps
    Task<IEnumerable<ConnectedApp>> ListAppsAsync(CancellationToken cancellationToken = default);
    Task RevokeAppAsync(System.Guid appId, CancellationToken cancellationToken = default);

    // Missing methods for perms
    Task GrantPermissionAsync(System.Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default);
    Task RevokePermissionAsync(System.Guid appId, GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default);
    Task RequestPermissionAsync(GameMaster.Client.Models.Resource resource, GameMaster.Client.Models.PermissionAction action, CancellationToken cancellationToken = default);
}
