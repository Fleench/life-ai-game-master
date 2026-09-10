using System.Data;
using System.Threading.Tasks;

namespace GameMaster.Data;
public interface IDbConnectionFactory {
    Task<IDbConnection> CreateConnectionAsync();
}
