using System;
using System.Threading.Tasks;
using Dapper;
using NotesApp.Api.Data;
using NotesApp.Api.Models;

namespace NotesApp.Api.Repositories
{
    /// <summary>
    /// Raw, parameterized Dapper queries against the "users" table (SQL Server).
    /// No ORM change-tracking; every statement is explicit SQL.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT id            AS Id,
                       email         AS Email,
                       display_name  AS DisplayName,
                       password_hash AS PasswordHash,
                       created_at    AS CreatedAt,
                       updated_at    AS UpdatedAt
                FROM dbo.users
                WHERE email = @Email;";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            const string sql = @"
                SELECT id            AS Id,
                       email         AS Email,
                       display_name  AS DisplayName,
                       password_hash AS PasswordHash,
                       created_at    AS CreatedAt,
                       updated_at    AS UpdatedAt
                FROM dbo.users
                WHERE id = @Id;";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.users WHERE email = @Email)
                       THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return await connection.ExecuteScalarAsync<bool>(sql, new { Email = email });
        }

        public async Task<User> CreateAsync(string email, string displayName, string passwordHash)
        {
            // SQL Server has no RETURNING clause — OUTPUT INSERTED.* serves the same purpose.
            const string sql = @"
                DECLARE @NewId UNIQUEIDENTIFIER = NEWID();
                DECLARE @Now DATETIME2 = SYSUTCDATETIME();

                INSERT INTO dbo.users (id, email, display_name, password_hash, created_at, updated_at)
                OUTPUT inserted.id            AS Id,
                       inserted.email         AS Email,
                       inserted.display_name  AS DisplayName,
                       inserted.password_hash AS PasswordHash,
                       inserted.created_at    AS CreatedAt,
                       inserted.updated_at    AS UpdatedAt
                VALUES (@NewId, @Email, @DisplayName, @PasswordHash, @Now, @Now);";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return await connection.QuerySingleAsync<User>(sql, new
            {
                Email = email,
                DisplayName = displayName,
                PasswordHash = passwordHash
            });
        }
    }
}
