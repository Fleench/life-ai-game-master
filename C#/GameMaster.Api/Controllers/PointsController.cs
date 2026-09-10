using GameMaster.Api.DTOs;
using GameMaster.Api.Filters;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class PointsController : ControllerBase
{
    private readonly IPointsService _pointsService;
    private readonly IPermissionsService _permissionsService;

    public PointsController(IPointsService pointsService, IPermissionsService permissionsService)
    {
        _pointsService = pointsService;
        _permissionsService = permissionsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPoints()
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            var hasExpRead = await _permissionsService.CheckAsync(app.AppId, Resource.ExpPoints, PermissionAction.Read);
            var hasCoinsRead = await _permissionsService.CheckAsync(app.AppId, Resource.Coins, PermissionAction.Read);

            if (!hasExpRead && !hasCoinsRead)
            {
                return StatusCode(403, new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = "App must have read permission on ExpPoints or Coins to get balances."
                });
            }

            var balances = await _pointsService.GetBalancesAsync();
            return Ok(balances);
        }
        
        return Unauthorized();
    }

    [HttpPost("award")]
    public async Task<IActionResult> AwardPoints([FromBody] PointsRequest request)
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            var hasPerm = await _permissionsService.CheckAsync(app.AppId, request.Resource, PermissionAction.Award);
            if (!hasPerm)
            {
                return StatusCode(403, new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = $"App does not have Award permission for {request.Resource}."
                });
            }

            await _pointsService.AwardPointsAsync(request.Resource.ToString().ToLowerInvariant(), request.Amount);
            var balances = await _pointsService.GetBalancesAsync();
            return Ok(balances);
        }
        
        return Unauthorized();
    }

    [HttpPost("spend")]
    public async Task<IActionResult> SpendPoints([FromBody] PointsRequest request)
    {
         if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            var hasPerm = await _permissionsService.CheckAsync(app.AppId, request.Resource, PermissionAction.Spend);
            if (!hasPerm)
            {
                return StatusCode(403, new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = $"App does not have Spend permission for {request.Resource}."
                });
            }

            await _pointsService.SpendPointsAsync(request.Resource.ToString().ToLowerInvariant(), request.Amount);
            var balances = await _pointsService.GetBalancesAsync();
            return Ok(balances);
        }
        
        return Unauthorized();
    }
}
