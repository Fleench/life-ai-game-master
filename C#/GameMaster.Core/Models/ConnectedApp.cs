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
