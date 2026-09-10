using System;
namespace GameMaster.Core.Models;
public class AppPermission {
    public Guid AppId { get; set; }
    public Resource Resource { get; set; }
    public PermissionAction Action { get; set; }
    public bool Granted { get; set; }
    public string Status { get; set; } = "granted";
    public DateTime UpdatedAt { get; set; }
}
