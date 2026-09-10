using GameMaster.Api.DTOs;
using GameMaster.Api.Filters;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [PermissionGuard(Resource.Inventory, PermissionAction.Read)]
    public async Task<IActionResult> GetInventory()
    {
        var items = await _inventoryService.GetItemsAsync();
        return Ok(items);
    }

    [HttpPost("add")]
    [PermissionGuard(Resource.Inventory, PermissionAction.Award)]
    public async Task<IActionResult> AddItem([FromBody] AddInventoryItemRequest request)
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            await _inventoryService.AddItemAsync(request.Name, request.Quantity, request.Metadata, app.AppId.ToString());
            return Ok();
        }
        return Unauthorized();
    }

    [HttpPost("remove")]
    [PermissionGuard(Resource.Inventory, PermissionAction.Spend)]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveInventoryItemRequest request)
    {
        await _inventoryService.RemoveItemAsync(request.ItemId, request.Quantity);
        return Ok();
    }
}
