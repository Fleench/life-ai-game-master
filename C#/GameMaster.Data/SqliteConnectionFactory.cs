using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace GameMaster.Data;
public class SqliteConnectionFactory : IDbConnectionFactory {
    private readonly string _connectionString;
    public SqliteConnectionFactory(string connectionString) {
        _connectionString = connectionString;
    }
    public async Task<IDbConnection> CreateConnectionAsync() {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
