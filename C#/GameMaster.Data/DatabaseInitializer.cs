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
    }
}
