using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using GameMaster.Data;
using GameMaster.Core.Services;
using GameMaster.Data.Services;

namespace GameMaster.Tests;
public class TestDatabaseFixture : IDisposable {
    public SqliteConnection Connection { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }
    
    public TestDatabaseFixture() {
        Connection = new SqliteConnection("DataSource=file:testdb?mode=memory&cache=shared");
        Connection.Open();
        
        var services = new ServiceCollection();
        // Use standard factory with shared in-memory database
        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory("DataSource=file:testdb?mode=memory&cache=shared"));
        services.AddTransient<DatabaseInitializer>();
        services.AddScoped<IPlayerService, SqlitePlayerService>();
        services.AddScoped<IPointsService, SqlitePointsService>();
        services.AddScoped<IInventoryService, SqliteInventoryService>();
        services.AddScoped<IAppRegistryService, SqliteAppRegistryService>();
        services.AddScoped<IPermissionsService, SqlitePermissionsService>();
        services.AddScoped<ISyncService, SqliteSyncService>();
        
        ServiceProvider = services.BuildServiceProvider();
        
        Dapper.SqlMapper.RemoveTypeMap(typeof(Guid));
        Dapper.SqlMapper.AddTypeHandler(new GameMaster.Data.GuidTypeHandler());
        
        // Mock the file read by executing the raw SQL directly
        var sql = @"
            CREATE TABLE IF NOT EXISTS players (id TEXT PRIMARY KEY, display_name TEXT NOT NULL, created_at DATETIME NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS currency_pools (currency_id TEXT PRIMARY KEY, balance INTEGER NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS inventory_items (item_id TEXT PRIMARY KEY, name TEXT NOT NULL, quantity INTEGER NOT NULL, metadata TEXT NOT NULL, awarded_by_app TEXT NOT NULL, created_at DATETIME NOT NULL, updated_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS connected_apps (app_id TEXT PRIMARY KEY, app_name TEXT NOT NULL, platform INTEGER NOT NULL, api_key_hash TEXT NOT NULL, android_uid INTEGER, registered_at DATETIME NOT NULL, last_seen_at DATETIME NOT NULL);
            CREATE TABLE IF NOT EXISTS app_permissions (app_id TEXT NOT NULL, resource INTEGER NOT NULL, action INTEGER NOT NULL, granted INTEGER NOT NULL, updated_at DATETIME NOT NULL, PRIMARY KEY (app_id, resource, action));
            CREATE TABLE IF NOT EXISTS paired_devices (device_id TEXT PRIMARY KEY, device_name TEXT NOT NULL, platform INTEGER NOT NULL, pair_code_hash TEXT NOT NULL, last_sync_at DATETIME NOT NULL, sync_vector_clock TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS sync_log (id INTEGER PRIMARY KEY AUTOINCREMENT, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, conflict_details TEXT NOT NULL, logged_at DATETIME NOT NULL);
        ";
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
    
    public void Dispose() {
        Connection.Close();
        Connection.Dispose();
    }
}

