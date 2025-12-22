using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    public sealed class SqlServerMigrationHistoryRepository : IMigrationHistoryRepository
    {
        private readonly SqlServerMigrationSession _s;
        private readonly string _schema;
        private readonly string _table;

        public SqlServerMigrationHistoryRepository(SqlServerMigrationSession session, string schema = "dbo", string table = "__BareORMMigrationsHistory")
        {
            _s = session;
            _schema = schema;
            _table = table;
        }

        public void EnsureCreated()
        {
            // schema
            _s.ExecuteNonQuery($@"
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{Lit(_schema)}')
                EXEC(N'CREATE SCHEMA [{Ident(_schema)}]');
            ");

            // table
            _s.ExecuteNonQuery($@"
            IF OBJECT_ID(N'{Lit(_schema)}.{Lit(_table)}', N'U') IS NULL
            BEGIN
                CREATE TABLE [{Ident(_schema)}].[{Ident(_table)}]
                (
                    MigrationId     NVARCHAR(64)  NOT NULL,
                    Name            NVARCHAR(200) NOT NULL,
                    ProductVersion  NVARCHAR(64)  NOT NULL,
                    AppliedAtUtc    DATETIME2     NOT NULL,
                    CONSTRAINT [PK_{Ident(_table)}] PRIMARY KEY (MigrationId)
                );
            END
            ");
        }

        public IReadOnlySet<string> GetAppliedMigrationIds()
        {
            var ids = _s.QueryStrings($@"SELECT MigrationId FROM [{Ident(_schema)}].[{Ident(_table)}] ORDER BY MigrationId;");
            return new HashSet<string>(ids, StringComparer.Ordinal);
        }

        public void Insert(string migrationId, string name, string productVersion, DateTime appliedAtUtc)
        {
            var sql = $@"
                INSERT INTO [{Ident(_schema)}].[{Ident(_table)}]
                (MigrationId, Name, ProductVersion, AppliedAtUtc)
                VALUES
                (N'{Lit(migrationId)}', N'{Lit(name)}', N'{Lit(productVersion)}', '{appliedAtUtc:yyyy-MM-ddTHH:mm:ss.fffffff}');";

            _s.ExecuteNonQuery(sql);
        }

        private static string Lit(string s) => s.Replace("'", "''");
        private static string Ident(string s) => s.Replace("]", "]]");
    }
}
