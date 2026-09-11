using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayer()
    {
        if (HttpContext.Items.TryGetValue("ConnectedApp", out var appObj) && appObj is ConnectedApp app)
        {
            var player = await _playerService.GetPlayerAsync();
            return Ok(player);
        }
        
        return Unauthorized();
    }
}
