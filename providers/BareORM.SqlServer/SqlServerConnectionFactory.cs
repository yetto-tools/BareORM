using System.Data.Common;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;

namespace BareORM.SqlServer
{
    public sealed class SqlServerConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}