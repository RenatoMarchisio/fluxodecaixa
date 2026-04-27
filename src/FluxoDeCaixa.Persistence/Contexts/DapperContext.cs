using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FluxoDeCaixa.Persistence.Contexts
{
    public class DapperContextFC
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContextFC(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _connectionString = _configuration.GetConnectionString("FluxoDeCaixaConnection") ?? throw new ArgumentNullException("Connection string not found.");
        }
        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
