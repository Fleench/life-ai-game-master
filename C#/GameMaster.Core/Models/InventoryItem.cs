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
