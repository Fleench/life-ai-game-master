using GameMaster.Api.DTOs;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class PointsController : ControllerBase
{
    private readonly IPointsService _pointsService;

    public PointsController(IPointsService pointsService)
    {
        _pointsService = pointsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPoints()
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
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
            await _pointsService.SpendPointsAsync(request.Resource.ToString().ToLowerInvariant(), request.Amount);
            var balances = await _pointsService.GetBalancesAsync();
            return Ok(balances);
        }
        
        return Unauthorized();
    }
}
