using System.ComponentModel.DataAnnotations;
using GameMaster.Core.Models;

namespace GameMaster.Api.DTOs;

public record RegisterAppRequest([Required] string AppName, [Required] Platform Platform);

public record PointsRequest([Required] Resource Resource, [Required][Range(1, int.MaxValue)] int Amount);

public record AddInventoryItemRequest([Required] string Name, [Required][Range(1, int.MaxValue)] int Quantity, string? Metadata);

public record RemoveInventoryItemRequest([Required] Guid ItemId, [Required][Range(1, int.MaxValue)] int Quantity);
