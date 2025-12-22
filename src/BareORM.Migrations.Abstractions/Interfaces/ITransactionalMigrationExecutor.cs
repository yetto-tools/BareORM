using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Migrations.Abstractions.Interfaces
{
    public interface ITransactionalMigrationExecutor : IMigrationExecutor
    {
        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}
