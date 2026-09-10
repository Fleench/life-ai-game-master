using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IAppRegistryService {
    Task<(ConnectedApp App, string PlainTextKey)> RegisterAppAsync(string appName, Platform platform, int? androidUid = null);
    Task<IEnumerable<ConnectedApp>> ListAppsAsync();
    Task DeregisterAppAsync(Guid appId);
    Task UpdateLastSeenAsync(Guid appId);
    Task<ConnectedApp?> GetAppByApiKeyHashAsync(string apiKeyHash);
    Task<ConnectedApp?> GetAppByAndroidUidAsync(int uid);
    Task<string> RotateApiKeyAsync(Guid appId);
}
