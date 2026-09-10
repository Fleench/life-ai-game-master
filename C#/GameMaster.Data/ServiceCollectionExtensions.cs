using Microsoft.Extensions.DependencyInjection;
using GameMaster.Core.Services;
using GameMaster.Data.Services;

namespace GameMaster.Data;
public static class ServiceCollectionExtensions {
    public static IServiceCollection AddGameMasterData(this IServiceCollection services, string connectionString) {
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(connectionString));
        services.AddTransient<DatabaseInitializer>();
        services.AddScoped<IPlayerService, SqlitePlayerService>();
        services.AddScoped<IPointsService, SqlitePointsService>();
        services.AddScoped<IInventoryService, SqliteInventoryService>();
        services.AddScoped<IAppRegistryService, SqliteAppRegistryService>();
        services.AddScoped<IPermissionsService, SqlitePermissionsService>();
        services.AddScoped<ISyncService, SqliteSyncService>();
        return services;
    }
}
