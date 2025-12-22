using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Core;

namespace BareORM.Migrations.Abstractions.Interfaces
{
    public sealed class BareOrmMigrationExecutor : IMigrationExecutor
    {
        private readonly DbContextLite _db;
        public BareOrmMigrationExecutor(DbContextLite db) => _db = db;

        public void ExecuteBatch(string sql, int timeoutSeconds = 120)
            => _db.Execute(sql, CommandType.Text, parameters: null, timeoutSeconds: timeoutSeconds);
    }
}
