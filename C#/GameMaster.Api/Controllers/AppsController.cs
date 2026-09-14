using GameMaster.Api.DTOs;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AppsController : ControllerBase
{
    private readonly IAppRegistryService _appRegistryService;

    public AppsController(IAppRegistryService appRegistryService)
    {
        _appRegistryService = appRegistryService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var apps = await _appRegistryService.ListAppsAsync();
        return Ok(apps);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAppRequest request)
    {
        var (app, apiKey) = await _appRegistryService.RegisterAppAsync(request.AppName, request.Platform);
        return Ok(new { App = app, ApiKey = apiKey });
    }

    [HttpPost("rotate-key")]
    public async Task<IActionResult> RotateKey()
    {
        var app = HttpContext.Items["ConnectedApp"] as ConnectedApp;
        if (app == null) return Unauthorized();
        var newKey = await _appRegistryService.RotateApiKeyAsync(app.AppId);
        return Ok(new { ApiKey = newKey });
    }

    [HttpDelete("{appId:guid}")]
    public async Task<IActionResult> Delete(Guid appId)
    {
        await _appRegistryService.DeregisterAppAsync(appId);
        return NoContent();
    }
}
