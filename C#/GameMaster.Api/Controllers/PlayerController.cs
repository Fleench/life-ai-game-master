using GameMaster.Api.Filters;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IPermissionsService _permissionsService;

    public PlayerController(IPlayerService playerService, IPermissionsService permissionsService)
    {
        _playerService = playerService;
        _permissionsService = permissionsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayer()
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            // Read on ANY resource is sufficient for player info according to plan.
            var hasExpRead = await _permissionsService.CheckAsync(app.AppId, Resource.ExpPoints, PermissionAction.Read);
            var hasCoinsRead = await _permissionsService.CheckAsync(app.AppId, Resource.Coins, PermissionAction.Read);
            var hasInventoryRead = await _permissionsService.CheckAsync(app.AppId, Resource.Inventory, PermissionAction.Read);

            if (!hasExpRead && !hasCoinsRead && !hasInventoryRead)
            {
                return StatusCode(403, new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = "App must have read permission on at least one resource to access player profile."
                });
            }

            var player = await _playerService.GetPlayerAsync();
            return Ok(player);
        }
        
        return Unauthorized();
    }
}
