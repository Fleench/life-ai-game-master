using System;

namespace GameMaster.Client.Models;

public enum Platform { Desktop, Android, IosRemote }
public enum Resource { ExpPoints, Coins, Inventory }
public enum PermissionAction { Read, Award, Spend, Manage }

public record RegisterAppRequest(string AppName, Platform Platform);

public record PointsRequest(Resource Resource, int Amount);

public record AddInventoryItemRequest(string Name, int Quantity, string? Metadata = null);

public record RemoveInventoryItemRequest(Guid ItemId, int Quantity);

public class ConnectedApp
{
    public Guid AppId { get; set; }
    public string AppName { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public int? AndroidUid { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public class RegisterResponse
{
    public ConnectedApp App { get; set; } = new();
    public string ApiKey { get; set; } = string.Empty;
}

public class CurrencyPool
{
    public string CurrencyId { get; set; } = string.Empty;
    public int Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Player
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class InventoryItem
{
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Metadata { get; set; }
    public string? AwardedByApp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AppPermission
{
    public Guid AppId { get; set; }
    public Resource Resource { get; set; }
    public PermissionAction Action { get; set; }
    public bool Granted { get; set; }
    public DateTime UpdatedAt { get; set; }
}
