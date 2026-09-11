#!/bin/bash
mkdir -p GameMaster.Core/Models GameMaster.Core/Services GameMaster.Core/Exceptions

# Models
cat << 'C_EOF' > GameMaster.Core/Models/Player.cs
using System;
namespace GameMaster.Core.Models;
public class Player {
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/CurrencyPool.cs
using System;
namespace GameMaster.Core.Models;
public class CurrencyPool {
    public string CurrencyId { get; set; } = string.Empty;
    public int Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/InventoryItem.cs
using System;
namespace GameMaster.Core.Models;
public class InventoryItem {
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Metadata { get; set; } = string.Empty;
    public string AwardedByApp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/Platform.cs
namespace GameMaster.Core.Models;
public enum Platform {
    Desktop,
    Android,
    IosRemote
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/ConnectedApp.cs
using System;
namespace GameMaster.Core.Models;
public class ConnectedApp {
    public Guid AppId { get; set; }
    public string AppName { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public int? AndroidUid { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/Resource.cs
namespace GameMaster.Core.Models;
public enum Resource {
    ExpPoints,
    Coins,
    Inventory,
    PhysicalExp,
    MentalExp,
    EmotionalExp,
    SocialExp,
    SpiritualExp
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/PermissionAction.cs
namespace GameMaster.Core.Models;
public enum PermissionAction {
    Read,
    Award,
    Spend,
    Manage
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/AppPermission.cs
using System;
namespace GameMaster.Core.Models;
public class AppPermission {
    public Guid AppId { get; set; }
    public Resource Resource { get; set; }
    public PermissionAction Action { get; set; }
    public bool Granted { get; set; }
    public DateTime UpdatedAt { get; set; }
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Models/PairedDevice.cs
using System;
namespace GameMaster.Core.Models;
public class PairedDevice {
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string PairCodeHash { get; set; } = string.Empty;
    public DateTime LastSyncAt { get; set; }
    public string SyncVectorClock { get; set; } = string.Empty;
}
C_EOF

# Exceptions
cat << 'C_EOF' > GameMaster.Core/Exceptions/GameMasterExceptions.cs
using System;
namespace GameMaster.Core.Exceptions;

public class InsufficientPointsException : Exception {
    public InsufficientPointsException(string message) : base(message) {}
}
public class PermissionDeniedException : Exception {
    public PermissionDeniedException(string message) : base(message) {}
}
public class AppNotFoundException : Exception {
    public AppNotFoundException(string message) : base(message) {}
}
public class DeviceNotFoundException : Exception {
    public DeviceNotFoundException(string message) : base(message) {}
}
C_EOF

# Services
cat << 'C_EOF' > GameMaster.Core/Services/IPlayerService.cs
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IPlayerService {
    Task<Player?> GetPlayerAsync();
    Task UpdatePlayerAsync(Player player);
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Services/IPointsService.cs
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
C_EOF

cat << 'C_EOF' > GameMaster.Core/Services/IInventoryService.cs
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
C_EOF

cat << 'C_EOF' > GameMaster.Core/Services/IAppRegistryService.cs
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
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Services/IPermissionsService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface IPermissionsService {
    Task GrantAsync(Guid appId, Resource resource, PermissionAction action);
    Task RevokeAsync(Guid appId, Resource resource, PermissionAction action);
    Task<bool> CheckAsync(Guid appId, Resource resource, PermissionAction action);
    Task<IEnumerable<AppPermission>> GetPermissionsAsync(Guid appId);
}
C_EOF

cat << 'C_EOF' > GameMaster.Core/Services/ISyncService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface ISyncService {
    Task<string> GeneratePairingCodeAsync();
    Task ConfirmPairAsync(string code, Guid remoteDeviceId, string remotePublicKey);
    Task UnpairAsync(Guid deviceId);
    Task TriggerSyncAsync(Guid deviceId);
    Task<IEnumerable<PairedDevice>> GetSyncStatusAsync();
    Task PushStateAsync(string delta);
    Task<string> GetStateSnapshotAsync();
}
C_EOF

