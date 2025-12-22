using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    public sealed class SqlServerMigrationExecutor : ITransactionalMigrationExecutor
    {
        private readonly SqlServerMigrationSession _s;

        public SqlServerMigrationExecutor(SqlServerMigrationSession session) => _s = session;

        public void ExecuteBatch(string sql, int timeoutSeconds = 120)
            => _s.ExecuteNonQuery(sql, timeoutSeconds);

        public void BeginTransaction() => _s.BeginTransaction();
        public void Commit() => _s.Commit();
        public void Rollback() => _s.Rollback();
    }
}
