using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    public sealed class SqlServerMigrationLockProvider : IMigrationLockProvider
    {
        private readonly SqlServerMigrationSession _s;
        private readonly int _timeoutMs;

        public SqlServerMigrationLockProvider(SqlServerMigrationSession session, int timeoutMs = 30_000)
        {
            _s = session;
            _timeoutMs = timeoutMs;
        }

        public IMigrationLock Acquire(string scope)
        {
            var sql = $@"
                    DECLARE @res INT;
                    EXEC @res = sp_getapplock
                        @Resource = N'{Lit(scope)}',
                        @LockMode = 'Exclusive',
                        @LockOwner = 'Session',
                        @LockTimeout = {_timeoutMs};
                    SELECT @res;";

            var resObj = _s.ExecuteScalar(sql);
            var res = Convert.ToInt32(resObj);

            if (res < 0)
                throw new InvalidOperationException($"sp_getapplock failed for '{scope}', code={res}");

            return new SqlServerMigrationLock(_s, scope);
        }

        private sealed class SqlServerMigrationLock : IMigrationLock
        {
            private readonly SqlServerMigrationSession _s;
            private readonly string _scope;
            private bool _disposed;

            public SqlServerMigrationLock(SqlServerMigrationSession s, string scope)
            {
                _s = s;
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _s.ExecuteNonQuery($@"
EXEC sp_releaseapplock
    @Resource = N'{Lit(_scope)}',
    @LockOwner = 'Session';");
            }
        }

        private static string Lit(string s) => s.Replace("'", "''");
    }
}
