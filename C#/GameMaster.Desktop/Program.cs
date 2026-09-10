using System.Text.Json;
using System.Threading.RateLimiting;
using GameMaster.Core.Services;
using GameMaster.Data;
using GameMaster.Desktop.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.IO;

var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gamemaster", "logs", "gamemaster-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // options.Filters.Add<PermissionGuardFilter>(); // Could use filter or middleware
});
builder.Services.AddEndpointsApiExplorer();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var apiKey = context.Request.Headers["X-Api-Key"].ToString();
        var key = string.IsNullOrEmpty(apiKey) ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown" : apiKey;
        
        return RateLimitPartition.GetFixedWindowLimiter(key, partition => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(10)
        });
    });
});

// Configure Kestrel port to 7777 and bind to any IP
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(7777);
});

// Register Core services from GameMaster.Data
builder.Services.AddGameMasterData("Data Source=gamemaster.db");

var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var init = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await init.InitializeAsync();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var problemDetails = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = "An error occurred while processing your request.",
            Detail = exception?.Message
        };

        if (exception is GameMaster.Core.Exceptions.PermissionDeniedException)
        {
            problemDetails.Status = 403;
            context.Response.StatusCode = 403;
        }
        else if (exception is GameMaster.Core.Exceptions.AppNotFoundException || exception is GameMaster.Core.Exceptions.DeviceNotFoundException)
        {
            problemDetails.Status = 404;
            context.Response.StatusCode = 404;
        }
        else if (exception is GameMaster.Core.Exceptions.InsufficientPointsException)
        {
            problemDetails.Status = 400;
            context.Response.StatusCode = 400;
        }
        else if (exception != null)
        {
            problemDetails.Status = 500;
            context.Response.StatusCode = 500;
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseRateLimiter();

// API Key Auth Middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

app.Run();

// For Integration Tests
public partial class Program { }
