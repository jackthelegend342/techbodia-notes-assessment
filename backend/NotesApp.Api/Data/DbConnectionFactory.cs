using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NotesApp.Api.Data
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateOpenConnectionAsync();
    }

    public class SqlServerConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException(
                    "Missing 'ConnectionStrings:SqlServer' configuration value.");
        }

        public async Task<IDbConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
