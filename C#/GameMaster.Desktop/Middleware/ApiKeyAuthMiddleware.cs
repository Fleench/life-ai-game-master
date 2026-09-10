using System.Security.Cryptography;
using System.Text;
using GameMaster.Core.Exceptions;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Desktop.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAppRegistryService appRegistryService)
    {
        // Skip auth for registration
        if (context.Request.Path.StartsWithSegments("/v1/apps/register"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
        {
            await WriteProblemDetailsAsync(context, 401, "API Key was not provided.");
            return;
        }

        var rawKey = extractedApiKey.ToString();
        var apps = await appRegistryService.ListAppsAsync();
        var app = apps.FirstOrDefault(a => BCrypt.Net.BCrypt.Verify(rawKey, a.ApiKeyHash));

        if (app == null)
        {
            await WriteProblemDetailsAsync(context, 401, "Unauthorized client.");
            return;
        }

        context.Items["ConnectedApp"] = app;

        await _next(context);
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Unauthorized",
            Detail = detail
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
