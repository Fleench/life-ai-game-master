using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace GameMaster.Data;
public class DatabaseInitializer {
    private readonly IDbConnectionFactory _connectionFactory;
    public DatabaseInitializer(IDbConnectionFactory connectionFactory) {
        SqlMapper.RemoveTypeMap(typeof(Guid)); SqlMapper.AddTypeHandler(new GuidTypeHandler());
        _connectionFactory = connectionFactory;
    }
    public async Task InitializeAsync() {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        
        // Read migration file as embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "GameMaster.Data.Migrations.001_initial_schema.sql";
        string sql = "";
        
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    sql = await reader.ReadToEndAsync();
                }
            }
            else
            {
                Console.WriteLine($"Failed to load resource: {resourceName}");
            }
        }
        
        if(!string.IsNullOrEmpty(sql)) {
            await conn.ExecuteAsync(sql);
        }

        // Run migration 003 - wrap in try/catch for idempotency
        try {
            var migration003 = "GameMaster.Data.Migrations.003_app_icons.sql";
            using (Stream stream3 = assembly.GetManifestResourceStream(migration003)) {
                if (stream3 != null) {
                    using var reader3 = new StreamReader(stream3);
                    var sql3 = await reader3.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(sql3)) await conn.ExecuteAsync(sql3);
                }
            }
        } catch (Exception ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) {
            // Column already added in a previous run — safe to ignore
        }
    }
}
