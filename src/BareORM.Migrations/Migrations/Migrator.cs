
using BareORM.Migrations.Abstractions;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.Migrations.Migrations
{
    public sealed class Migrator
    {
        private readonly IMigrationSqlGenerator _sql;
        private readonly IMigrationHistoryRepository _history;
        private readonly IMigrationLockProvider _lockProvider;
        private readonly IMigrationExecutor _exec;
        private readonly MigratorOptions _opt;

        public Migrator(
            IMigrationSqlGenerator sql,
            IMigrationHistoryRepository history,
            IMigrationLockProvider lockProvider,
            IMigrationExecutor executor,
            MigratorOptions? options = null)
        {
            _sql = sql;
            _history = history;
            _lockProvider = lockProvider;
            _exec = executor;
            _opt = options ?? new MigratorOptions();
        }

        public void Migrate(IEnumerable<Migration> migrations)
        {
            _history.EnsureCreated();

            using var l = _lockProvider.Acquire(_opt.Scope);

            var applied = _history.GetAppliedMigrationIds();

            foreach (var m in migrations.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (applied.Contains(m.Id)) continue;

                var mb = new MigrationBuilder();
                m.Up(mb);

                var batches = _sql.Generate(mb.Operations);

                foreach (var sql in batches)
                    _exec.ExecuteBatch(sql, _opt.CommandTimeoutSeconds);

                _history.Insert(m.Id, m.Name, _opt.ProductVersion, DateTime.UtcNow);
            }
        }
    }
}
